"""One-off: split GlobalMasterDataEntities.cs and ApartmentAccountingModels.cs into one file per class."""
import re
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent / "Models"
USING = """using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;
"""


def split_by_table(text: str, out_dir: Path, class_using: str) -> list[str]:
    """Split file content that contains multiple [Table] classes (namespace stripped)."""
    # Drop namespace block if present
    if "public sealed class" in text and "[Table" in text:
        start = text.find("[Table(")
        text = text[start:]
    parts = re.split(r"(?=\[Table\()", text)
    written = []
    for p in parts:
        p = p.strip()
        if not p.startswith("[Table("):
            continue
        cm = re.search(r"public sealed class (\w+)", p)
        if not cm:
            continue
        name = cm.group(1)
        (out_dir / f"{name}.cs").write_text(class_using + "\n" + p + "\n", encoding="utf-8")
        written.append(name)
    return written


def main():
    g = split_by_table(
        (BASE / "GlobalMasterDataEntities.cs").read_text(encoding="utf-8"),
        BASE,
        USING.rstrip() + "\n",
    )
    print("Global master:", g)
    acc_using = """using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;
"""
    text = (BASE / "ApartmentAccountingModels.cs").read_text(encoding="utf-8")
    for name in split_by_table(text, BASE, acc_using.rstrip() + "\n"):
        print("Wrote", name)


if __name__ == "__main__":
    main()
