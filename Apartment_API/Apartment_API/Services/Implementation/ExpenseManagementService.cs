using System.Globalization;
using System.Text;
using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ExpenseManagementService(
    AppDbContext db,
    IWebHostEnvironment env) : IExpenseManagementService
{
    private static readonly decimal[] AllowedGst = [0m, 5m, 12m, 18m, 28m];

    public async Task<ExpenseSummaryDto> GetSummaryAsync(int apartmentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var (rangeFrom, rangeTo) = await ResolveRangeAsync(apartmentId, fromDate, toDate, cancellationToken);
        var rows = await db.ExpenseBills.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.BillDate >= rangeFrom && x.BillDate <= rangeTo)
            .ToListAsync(cancellationToken);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return new ExpenseSummaryDto
        {
            TotalBills = rows.Count,
            TotalAmount = rows.Sum(x => x.NetPayable),
            PendingApprovalCount = rows.Count(x => x.Status.StartsWith("PENDING_", StringComparison.OrdinalIgnoreCase)),
            PendingApprovalAmount = rows.Where(x => x.Status.StartsWith("PENDING_", StringComparison.OrdinalIgnoreCase)).Sum(x => x.NetPayable),
            ApprovedUnpaidCount = rows.Count(x => x.Status == "APPROVED" && (x.OutstandingAmount ?? 0m) > 0m),
            ApprovedUnpaidAmount = rows.Where(x => x.Status == "APPROVED" && (x.OutstandingAmount ?? 0m) > 0m).Sum(x => x.OutstandingAmount ?? 0m),
            PaidThisMonth = rows.Where(x => x.Status == "PAID" && x.UpdatedAt >= monthStart).Sum(x => x.AmountPaid),
            Period = rangeFrom.ToString("MMM yyyy", CultureInfo.InvariantCulture)
        };
    }

    public async Task<PagedResult<ExpenseBillListItemDto>> ListBillsAsync(
        int apartmentId, string? search, int? expenseHeadId, string? statusCode, int? vendorId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (rangeFrom, rangeTo) = await ResolveRangeAsync(apartmentId, fromDate, toDate, cancellationToken);
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = from b in db.ExpenseBills.AsNoTracking()
                where b.ApartmentId == apartmentId && b.BillDate >= rangeFrom && b.BillDate <= rangeTo
                join v in db.Vendors.AsNoTracking() on b.VendorId equals v.IdVendor
                join h in db.ExpenseHeads.AsNoTracking() on b.ExpenseHeadId equals h.IdExpenseHead
                join s in db.ExpenseStatuses.AsNoTracking() on b.Status equals s.StatusCode into sj
                from s in sj.DefaultIfEmpty()
                select new { b, v, h, s };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => (x.b.BillNumber ?? string.Empty).Contains(s) || x.v.VendorName.Contains(s));
        }
        if (expenseHeadId is { } eh) q = q.Where(x => x.b.ExpenseHeadId == eh);
        if (!string.IsNullOrWhiteSpace(statusCode)) q = q.Where(x => x.b.Status == statusCode!.Trim().ToUpperInvariant());
        if (vendorId is { } vid) q = q.Where(x => x.b.VendorId == vid);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderByDescending(x => x.b.BillDate).ThenByDescending(x => x.b.IdBill)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var items = rows.Select(x =>
        {
            var outstanding = x.b.OutstandingAmount ?? Math.Max(0m, x.b.NetPayable - x.b.AmountPaid);
            return new ExpenseBillListItemDto
            {
                BillId = x.b.IdBill,
                BillNumber = x.b.BillNumber ?? string.Empty,
                BillDate = x.b.BillDate,
                DueDate = x.b.DueDate,
                VendorId = x.b.VendorId,
                VendorName = x.v.VendorName,
                ExpenseHeadId = x.b.ExpenseHeadId,
                ExpenseHeadName = x.h.HeadName,
                GrossAmount = x.b.GrossAmount,
                TaxAmount = x.b.TaxAmount,
                TdsAmount = x.b.TdsAmount,
                NetPayable = x.b.NetPayable,
                AmountPaid = x.b.AmountPaid,
                OutstandingAmount = outstanding,
                StatusCode = x.b.Status,
                StatusName = x.s?.StatusName ?? x.b.Status,
                ApprovalTier = ResolveApprovalTier(x.b.NetPayable),
                HasJv = x.b.LockedInPeriodId != null,
                JvReference = x.b.LockedInPeriodId is { } p ? $"JV-{p}" : null,
                PaymentStatusLabel = outstanding <= 0 ? "Paid" : x.b.DueDate is { } d ? $"Due {d:dd MMM}" : "Unpaid",
                IsMigrated = false
            };
        }).ToList();

        return new PagedResult<ExpenseBillListItemDto> { Items = items, TotalCount = total, Page = pageNumber, PageSize = pageSize };
    }

    public async Task<object?> GetBillAsync(int apartmentId, long billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.ExpenseBills.AsNoTracking().FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBill == billId, cancellationToken);
        if (bill is null) return null;
        var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.IdVendor == bill.VendorId, cancellationToken);
        var statusName = await db.ExpenseStatuses.AsNoTracking().Where(x => x.StatusCode == bill.Status).Select(x => x.StatusName).FirstOrDefaultAsync(cancellationToken);
        var lines = await db.ExpenseBillLines.AsNoTracking().Where(x => x.BillId == billId).ToListAsync(cancellationToken);
        return new
        {
            billId = bill.IdBill,
            apartmentId = bill.ApartmentId,
            billNumber = bill.BillNumber,
            vendorId = bill.VendorId,
            vendorName = vendor?.VendorName,
            vendorGstNumber = vendor?.GstNumber,
            billDate = bill.BillDate,
            dueDate = bill.DueDate,
            description = bill.Description,
            budgetId = bill.BudgetId,
            grossAmount = bill.GrossAmount,
            taxAmount = bill.TaxAmount,
            tdsAmount = bill.TdsAmount,
            netPayable = bill.NetPayable,
            amountPaid = bill.AmountPaid,
            outstandingAmount = bill.OutstandingAmount ?? Math.Max(0m, bill.NetPayable - bill.AmountPaid),
            statusCode = bill.Status,
            statusName = statusName ?? bill.Status,
            approvalTier = ResolveApprovalTier(bill.NetPayable),
            attachmentFileName = bill.AttachmentFileName,
            lines = lines.Select(l => new
            {
                lineId = l.IdExpenseBillLine,
                expenseHeadId = l.ExpenseHeadId,
                isAsset = l.IsAsset,
                description = l.Description,
                hsnSacCode = l.HsnSacCode,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice,
                discount = l.Discount,
                taxableAmount = l.TaxableAmount,
                gstPercentage = l.GstPercentage,
                gstAmount = l.GstAmount,
                lineTotal = l.LineTotal
            }),
            approvalTrail = Array.Empty<object>(),
            payments = Array.Empty<object>(),
            createdBy = bill.CreatedBy,
            createdAt = bill.CreatedAt,
            updatedAt = bill.UpdatedAt
        };
    }

    public Task<ExpenseBillUpsertResponseDto> CreateContractAsync(int apartmentId, int userId, CreateContractExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var mapped = new CreateExpenseBillRequest
        {
            BillType = "CONTRACT",
            VendorId = request.VendorId,
            BillNumber = request.ContractReference,
            BillDate = request.ContractStartDate,
            DueDate = request.FirstPaymentDue,
            BudgetId = request.BudgetId,
            Description = request.Description,
            TdsAmount = request.TdsAmount,
            AttachmentFileName = request.AttachmentFileName,
            AttachmentBase64 = request.AttachmentBase64,
            Action = request.Action,
            Lines =
            [
                new ExpenseBillLineInputDto
                {
                    ExpenseHeadId = request.ExpenseHeadId,
                    IsAsset = request.IsAsset,
                    Description = request.Description,
                    HsnSacCode = request.HsnSacCode,
                    Quantity = 1m,
                    UnitPrice = request.TotalContractValue,
                    Discount = 0m,
                    GstPercentage = request.GstPercentage
                }
            ]
        };
        return CreateRegularAsync(apartmentId, userId, mapped, cancellationToken);
    }

    public async Task<ExpenseBillUpsertResponseDto> CreateRegularAsync(int apartmentId, int userId, CreateExpenseBillRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var vendorOk = await db.Vendors.AsNoTracking().AnyAsync(x => x.ApartmentId == apartmentId && x.IdVendor == request.VendorId && x.IsActive, cancellationToken);
        if (!vendorOk) throw new InvalidOperationException("VENDOR_NOT_FOUND");
        var duplicate = await db.ExpenseBills.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.VendorId == request.VendorId && x.BillNumber == request.BillNumber, cancellationToken);
        if (duplicate) throw new InvalidOperationException("DUPLICATE_BILL_NUMBER");

        var calc = Compute(request.Lines, request.TdsAmount, request.RetentionHeld, request.RoundOff);
        var status = request.Action.Trim().ToUpperInvariant() == "SUBMIT" ? "PENDING_L1" : "DRAFT";
        var now = DateTime.UtcNow;
        var bill = new ExpenseBill
        {
            ApartmentId = apartmentId,
            BillNumber = request.BillNumber.Trim(),
            VendorId = request.VendorId,
            ExpenseHeadId = request.Lines[0].ExpenseHeadId,
            BillDate = request.BillDate.Date,
            DueDate = request.DueDate?.Date,
            GrossAmount = calc.GrossAmount,
            TaxAmount = calc.TaxAmount,
            TdsAmount = request.TdsAmount,
            NetPayable = calc.NetPayable,
            AmountPaid = 0m,
            OutstandingAmount = calc.NetPayable,
            Status = status,
            Description = request.Description.Trim(),
            BudgetId = request.BudgetId,
            AttachmentFileName = SaveAttachment(request.AttachmentFileName, request.AttachmentBase64),
            CreatedBy = userId,
            CreatedAt = now,
            ModifiedBy = userId,
            UpdatedAt = now
        };
        db.ExpenseBills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);

        db.ExpenseBillLines.AddRange(calc.LineOutputs.Select((l, idx) => new ExpenseBillLine
        {
            BillId = bill.IdBill,
            ExpenseHeadId = request.Lines[idx].ExpenseHeadId,
            IsAsset = request.Lines[idx].IsAsset,
            Description = request.Lines[idx].Description.Trim(),
            HsnSacCode = string.IsNullOrWhiteSpace(request.Lines[idx].HsnSacCode) ? null : request.Lines[idx].HsnSacCode!.Trim(),
            Quantity = request.Lines[idx].Quantity,
            UnitPrice = request.Lines[idx].UnitPrice,
            Discount = request.Lines[idx].Discount,
            TaxableAmount = l.TaxableAmount,
            GstPercentage = request.Lines[idx].GstPercentage,
            GstAmount = l.GstAmount,
            LineTotal = l.LineTotal,
            CreatedBy = userId,
            CreatedAt = now
        }));
        await db.SaveChangesAsync(cancellationToken);

        return new ExpenseBillUpsertResponseDto
        {
            BillId = bill.IdBill,
            BillNumber = bill.BillNumber ?? string.Empty,
            NetPayable = bill.NetPayable,
            ApprovalTier = ResolveApprovalTier(bill.NetPayable),
            CurrentStatus = bill.Status,
            CurrentStatusName = bill.Status,
            Message = bill.Status == "DRAFT" ? "Expense bill saved as draft." : "Expense bill submitted for approval."
        };
    }

    public async Task<ExpenseBillUpsertResponseDto> UpdateDraftAsync(int apartmentId, int userId, long billId, CreateExpenseBillRequest request, CancellationToken cancellationToken = default)
    {
        var bill = await db.ExpenseBills.FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBill == billId, cancellationToken);
        if (bill is null) throw new InvalidOperationException("BILL_NOT_FOUND");
        if (bill.Status != "DRAFT") throw new InvalidOperationException("BILL_NOT_EDITABLE");
        ValidateRequest(request);
        var calc = Compute(request.Lines, request.TdsAmount, request.RetentionHeld, request.RoundOff);
        bill.BillNumber = request.BillNumber.Trim();
        bill.VendorId = request.VendorId;
        bill.ExpenseHeadId = request.Lines[0].ExpenseHeadId;
        bill.BillDate = request.BillDate.Date;
        bill.DueDate = request.DueDate?.Date;
        bill.GrossAmount = calc.GrossAmount;
        bill.TaxAmount = calc.TaxAmount;
        bill.TdsAmount = request.TdsAmount;
        bill.NetPayable = calc.NetPayable;
        bill.OutstandingAmount = Math.Max(0m, bill.NetPayable - bill.AmountPaid);
        bill.Status = request.Action.Trim().ToUpperInvariant() == "SUBMIT" ? "PENDING_L1" : "DRAFT";
        bill.Description = request.Description.Trim();
        bill.BudgetId = request.BudgetId;
        bill.UpdatedAt = DateTime.UtcNow;
        bill.ModifiedBy = userId;
        var old = await db.ExpenseBillLines.Where(x => x.BillId == billId).ToListAsync(cancellationToken);
        db.ExpenseBillLines.RemoveRange(old);
        db.ExpenseBillLines.AddRange(calc.LineOutputs.Select((l, idx) => new ExpenseBillLine
        {
            BillId = billId,
            ExpenseHeadId = request.Lines[idx].ExpenseHeadId,
            IsAsset = request.Lines[idx].IsAsset,
            Description = request.Lines[idx].Description.Trim(),
            HsnSacCode = request.Lines[idx].HsnSacCode,
            Quantity = request.Lines[idx].Quantity,
            UnitPrice = request.Lines[idx].UnitPrice,
            Discount = request.Lines[idx].Discount,
            TaxableAmount = l.TaxableAmount,
            GstPercentage = request.Lines[idx].GstPercentage,
            GstAmount = l.GstAmount,
            LineTotal = l.LineTotal,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        }));
        await db.SaveChangesAsync(cancellationToken);
        return new ExpenseBillUpsertResponseDto
        {
            BillId = bill.IdBill,
            BillNumber = bill.BillNumber ?? string.Empty,
            NetPayable = bill.NetPayable,
            ApprovalTier = ResolveApprovalTier(bill.NetPayable),
            CurrentStatus = bill.Status,
            CurrentStatusName = bill.Status,
            Message = bill.Status == "DRAFT" ? "Expense bill updated." : "Expense bill updated and submitted for approval."
        };
    }

    public async Task<ExpenseBillDeleteResponseDto> DeleteDraftAsync(int apartmentId, long billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.ExpenseBills.FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBill == billId, cancellationToken);
        if (bill is null) throw new InvalidOperationException("BILL_NOT_FOUND");
        if (bill.Status != "DRAFT") throw new InvalidOperationException("BILL_NOT_DELETABLE");
        var lines = await db.ExpenseBillLines.Where(x => x.BillId == billId).ToListAsync(cancellationToken);
        db.ExpenseBillLines.RemoveRange(lines);
        db.ExpenseBills.Remove(bill);
        await db.SaveChangesAsync(cancellationToken);
        return new ExpenseBillDeleteResponseDto { BillId = billId, Deleted = true, Message = "Draft expense bill deleted successfully." };
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> DownloadAttachmentAsync(int apartmentId, long billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.ExpenseBills.AsNoTracking().FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBill == billId, cancellationToken);
        if (bill is null || string.IsNullOrWhiteSpace(bill.AttachmentFileName)) throw new InvalidOperationException("ATTACHMENT_NOT_FOUND");
        var path = Path.Combine(env.ContentRootPath, "Uploads", "expenses", bill.AttachmentFileName);
        if (!File.Exists(path)) throw new InvalidOperationException("ATTACHMENT_NOT_FOUND");
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return (bytes, GuessMime(bill.AttachmentFileName), bill.AttachmentFileName);
    }

    public Task<ExpenseCalculateResponseDto> CalculateAsync(ExpenseCalculateRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var calc = Compute(request.Lines, request.TdsAmount, request.RetentionHeld, request.RoundOff);
        return Task.FromResult(new ExpenseCalculateResponseDto
        {
            Lines = calc.LineOutputs.Select(x => new ExpenseLineComputedDto { TaxableAmount = x.TaxableAmount, GstAmount = x.GstAmount, LineTotal = x.LineTotal }).ToList(),
            SubTotalTaxable = calc.GrossAmount,
            TotalGst = calc.TaxAmount,
            GrossAmount = calc.GrossAmount,
            TaxAmount = calc.TaxAmount,
            TdsAmount = request.TdsAmount,
            RetentionHeld = request.RetentionHeld,
            RoundOff = request.RoundOff,
            NetPayable = calc.NetPayable,
            ApprovalTier = ResolveApprovalTier(calc.NetPayable),
            ApprovalTierLabel = ResolveTierLabel(calc.NetPayable)
        });
    }

    public async Task<ExpenseBudgetCheckDto> BudgetCheckAsync(int apartmentId, int expenseHeadId, int fiscalYearId, decimal? additionalAmount, CancellationToken cancellationToken = default)
    {
        var budget = await db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.ExpenseHeadId == expenseHeadId && x.IsActive, cancellationToken);
        if (budget is null) throw new InvalidOperationException("Budget not found.");
        var headName = await db.ExpenseHeads.AsNoTracking().Where(x => x.IdExpenseHead == expenseHeadId).Select(x => x.HeadName).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var projected = budget.ActualAmount + (additionalAmount ?? 0m);
        var util = budget.BudgetAmount <= 0 ? 0m : Math.Round((budget.ActualAmount / budget.BudgetAmount) * 100m, 1);
        var projectedUtil = budget.BudgetAmount <= 0 ? 0m : Math.Round((projected / budget.BudgetAmount) * 100m, 1);
        var remaining = budget.BudgetAmount - budget.ActualAmount;
        var over = projectedUtil > 100m;
        var near = projectedUtil >= 90m && !over;
        return new ExpenseBudgetCheckDto
        {
            ExpenseHeadId = expenseHeadId,
            ExpenseHeadName = headName,
            BudgetAmount = budget.BudgetAmount,
            ActualSpent = budget.ActualAmount,
            CommittedAmount = 0m,
            BudgetRemaining = remaining,
            UtilisationPercent = util,
            ProjectedUtilisationPercent = projectedUtil,
            IsOverBudget = over,
            IsNearThreshold = near,
            WarningMessage = over ? "Projected entry exceeds budget." : near ? $"Budget {projectedUtil}% utilised after this entry." : null
        };
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportBillsAsync(
        int apartmentId, string format, string? search, int? expenseHeadId, string? statusCode, int? vendorId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var data = await ListBillsAsync(apartmentId, search, expenseHeadId, statusCode, vendorId, fromDate, toDate, 1, 1000, cancellationToken);
        var fmt = format.Trim().ToLowerInvariant();
        if (fmt is not ("xlsx" or "pdf")) throw new InvalidOperationException("format must be xlsx or pdf.");
        var sb = new StringBuilder();
        sb.AppendLine("BillNumber,BillDate,Vendor,Head,NetPayable,AmountPaid,Outstanding,Status");
        foreach (var x in data.Items) sb.AppendLine($"{Csv(x.BillNumber)},{x.BillDate:yyyy-MM-dd},{Csv(x.VendorName)},{Csv(x.ExpenseHeadName)},{x.NetPayable},{x.AmountPaid},{x.OutstandingAmount},{x.StatusCode}");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return fmt == "xlsx"
            ? (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ExpenseList_{DateTime.UtcNow:MMMyyyy}.xlsx")
            : (bytes, "application/pdf", $"ExpenseList_{DateTime.UtcNow:MMMyyyy}.pdf");
    }

    public async Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetVendorsAsync(int apartmentId, bool isActive, string? search, CancellationToken cancellationToken = default)
    {
        var q = db.Vendors.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (isActive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.VendorName.Contains(search.Trim()));
        return await q.OrderBy(x => x.VendorName).Take(200).Select(x => new ExpenseSimpleLookupDto
        {
            Id = x.IdVendor, Code = x.VendorCode, Name = x.VendorName, Category = x.ContactPerson, GstNumber = x.GstNumber
        }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetExpenseHeadsAsync(int apartmentId, bool isActive, CancellationToken cancellationToken = default)
    {
        var q = db.ExpenseHeads.AsNoTracking().Where(x => x.ApartmentId == null || x.ApartmentId == apartmentId);
        if (isActive) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.HeadName).Select(x => new ExpenseSimpleLookupDto
        {
            Id = x.IdExpenseHead, Code = x.HeadCode, Name = x.HeadName, GlCode = x.LedgerAccountId == null ? null : x.LedgerAccountId.Value.ToString(), FundSource = "GENERAL"
        }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetBankAccountsAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        var rows = await db.BankAccounts.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsActive)
            .OrderBy(x => x.AccountName).ToListAsync(cancellationToken);
        return rows.Select(x => new ExpenseSimpleLookupDto
            {
                Id = x.IdBankAccount,
                AccountName = x.AccountName,
                BankName = x.BankName,
                AccountNumberMasked = x.AccountNumber.Length <= 4 ? x.AccountNumber : $"****{x.AccountNumber[^4..]}",
                Code = string.Empty,
                Name = string.Empty
            }).ToList();
    }

    public async Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetFiscalYearsAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        return await db.FiscalYears.AsNoTracking().Where(x => x.ApartmentId == apartmentId && (x.IsActive ?? true))
            .OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.StartDate)
            .Select(x => new ExpenseSimpleLookupDto
            {
                Id = x.IdFiscalYear,
                Label = $"FY {x.StartDate:yyyy}-{x.EndDate:yy}",
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsCurrent = x.IsCurrent,
                Code = string.Empty,
                Name = string.Empty
            }).ToListAsync(cancellationToken);
    }

    private async Task<(DateTime From, DateTime To)> ResolveRangeAsync(int apartmentId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var to = (toDate ?? DateTime.UtcNow.Date).Date;
        var from = fromDate?.Date;
        if (from == null)
            from = await db.FiscalYears.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsCurrent && (x.IsActive ?? true)).Select(x => (DateTime?)x.StartDate).FirstOrDefaultAsync(ct)
                ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        if (from > to) throw new InvalidOperationException("INVALID_DATE_RANGE");
        return (from.Value, to);
    }

    private static void ValidateRequest(CreateExpenseBillRequest r)
    {
        if (r.Lines.Count == 0) throw new InvalidOperationException("NO_LINES");
        if (string.IsNullOrWhiteSpace(r.BillNumber) || string.IsNullOrWhiteSpace(r.Description)) throw new InvalidOperationException("Required fields are missing.");
        if (r.BudgetId <= 0) throw new InvalidOperationException("BudgetId is required.");
        foreach (var l in r.Lines)
        {
            if (l.Quantity <= 0 || l.UnitPrice < 0 || l.Discount < 0) throw new InvalidOperationException("Invalid line values.");
            if (!AllowedGst.Contains(l.GstPercentage)) throw new InvalidOperationException("INVALID_GST_RATE");
        }
    }

    private static (decimal GrossAmount, decimal TaxAmount, decimal NetPayable, IReadOnlyList<(decimal TaxableAmount, decimal GstAmount, decimal LineTotal)> LineOutputs) Compute(
        IReadOnlyList<ExpenseBillLineInputDto> lines, decimal tds, decimal retention, decimal roundOff)
    {
        var outLines = new List<(decimal, decimal, decimal)>(lines.Count);
        decimal gross = 0m;
        decimal tax = 0m;
        foreach (var l in lines)
        {
            var taxable = Math.Round((l.UnitPrice - l.Discount) * l.Quantity, 2, MidpointRounding.AwayFromZero);
            var gst = Math.Round(taxable * l.GstPercentage / 100m, 2, MidpointRounding.AwayFromZero);
            outLines.Add((taxable, gst, taxable + gst));
            gross += taxable;
            tax += gst;
        }
        var net = Math.Round(gross + tax - tds - retention + roundOff, 2, MidpointRounding.AwayFromZero);
        return (gross, tax, net, outLines);
    }

    private string? SaveAttachment(string? fileName, string? base64)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(base64)) return null;
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";
        var save = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(env.ContentRootPath, "Uploads", "expenses", save);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Convert.FromBase64String(base64));
        return save;
    }

    private static string ResolveApprovalTier(decimal net) => net <= 10000 ? "T1" : net <= 25000 ? "T2" : net <= 100000 ? "T3" : "T4";
    private static string ResolveTierLabel(decimal net) => ResolveApprovalTier(net) switch { "T1" => "Admin only", "T2" => "Admin -> MC Treasurer", "T3" => "Admin -> MC Treasurer -> MC President", _ => "Admin -> MC Full vote -> AGM minutes" };
    private static string GuessMime(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch { ".pdf" => "application/pdf", ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", _ => "application/octet-stream" };
    private static string Csv(string? s) => string.IsNullOrEmpty(s) ? "" : (s.Contains(',') || s.Contains('"') || s.Contains('\n')) ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

}
