#!/usr/bin/env python3
"""Etiketleme dogrulama listesini kaynaga karsi kontrol eder.

Asama 3'te ayni kontrol iki tur ust uste kusur yakalamisti, o yuzden liste
yazildiktan sonra elle degil programatik olarak kontrol ediliyor. Baktigi seyler:

  1. Iki sha da depoda var mi
  2. Yazilan dosya, duzeltme commit'inde gercekten degismis mi
  3. Yazilan satir araligi, duzeltmenin ebeveyn surumunde gercekten degismis mi
  4. Baglantilar dogru repoya ve dogru sha'ya gidiyor mu
  5. Suclanan commit, duzeltmeden once mi yazilmis

Python, cunku is markdown ayristirmak ve git cagirmak; urun kodunun parcasi degil.
Kullanim: python3 tools/liste-kontrol.py <liste.md> <repo-klasoru> <uzak-adres>
"""

import re
import subprocess
import sys

ROW = re.compile(
    r"^\|\s*\d+\s*\|\s*`([0-9a-f]+)`\s*\|(.*?)\|\s*`([0-9a-f]+)`\s*\|(.*?)\|\s*`(.+?)`\s*\|\s*([0-9-]+)\s*\|(.*?)\|"
)


def git(repo, *args):
    result = subprocess.run(
        ["git", "-C", repo, *args], capture_output=True, text=True, check=False
    )
    return result.stdout, result.returncode


def changed_lines(repo, sha, path):
    """Duzeltme commit'inde o dosyada silinen satirlarin ESKI numaralari."""
    text, code = git(repo, "show", "--unified=0", "--format=", sha, "--", path)
    if code != 0:
        return None

    lines = set()
    for line in text.splitlines():
        if not line.startswith("@@"):
            continue
        match = re.search(r"-(\d+)(?:,(\d+))?", line)
        if not match:
            continue
        start = int(match.group(1))
        count = int(match.group(2) or 1)
        lines.update(range(start, start + count))
    return lines


def main():
    list_path, repo, remote = sys.argv[1], sys.argv[2], sys.argv[3].rstrip("/")
    if remote.endswith(".git"):
        remote = remote[:-4]

    rows = 0
    problems = []
    labelled_section = True

    for raw in open(list_path, encoding="utf-8"):
        if raw.startswith("### "):
            # Iki bolumun kurali ters: etiketlenmis satirlarin araligi duzeltmede
            # DEGISMIS olmali, etiketlenmemis satirlarinki DEGISMEMIS olmali.
            labelled_section = "Etiketlenmemis" not in raw

        match = ROW.match(raw.strip())
        if not match:
            continue

        rows += 1
        fix, _, culprit, _, path, span, links = match.groups()

        for sha, label in ((fix, "duzeltme"), (culprit, "suclanan")):
            _, code = git(repo, "cat-file", "-e", sha + "^{commit}")
            if code != 0:
                problems.append(f"satir {rows}: {label} sha depoda yok ({sha})")

        for sha in (fix, culprit):
            if sha not in links:
                problems.append(f"satir {rows}: baglantida {sha} gecmiyor")
        if remote not in links:
            problems.append(f"satir {rows}: baglanti baska repoya gidiyor")

        touched = changed_lines(repo, fix, path)
        if touched is None:
            problems.append(f"satir {rows}: dosya duzeltmede degismemis ({path})")
            continue
        if not touched:
            problems.append(f"satir {rows}: duzeltmede silinen satir yok ({path})")

        bounds = [int(part) for part in span.split("-")]
        first, last = bounds[0], bounds[-1]
        overlaps = any(line in touched for line in range(first, last + 1))

        if labelled_section and not overlaps:
            problems.append(
                f"satir {rows}: etiketli satirin araligi {first}-{last} duzeltmede degismemis ({path})"
            )

        if not labelled_section and overlaps:
            problems.append(
                f"satir {rows}: etiketsiz satirin araligi {first}-{last} duzeltmede degismis ({path})"
            )

        fix_date, _ = git(repo, "show", "-s", "--format=%at", fix)
        culprit_date, _ = git(repo, "show", "-s", "--format=%at", culprit)
        if fix_date.strip() and culprit_date.strip():
            if int(culprit_date) > int(fix_date):
                problems.append(f"satir {rows}: suclanan commit duzeltmeden yeni")

    print(f"kontrol edilen satir: {rows}")
    print(f"sapma: {len(problems)}")
    for problem in problems:
        print(f"  - {problem}")


if __name__ == "__main__":
    main()
