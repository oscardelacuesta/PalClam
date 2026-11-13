# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
"""Validación estática portable. No ejecuta C#, WPF, ClamAV ni PowerShell.

Uso: python Tools/validate_localization.py
Requiere únicamente Python 3. No escribe en datos de usuario.
"""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
LANGUAGES = ("es", "en", "fr", "de", "pt", "it")


def read_resources(path):
    values = {}
    for node in ET.parse(path).getroot().findall("data"):
        key, value = node.get("name"), node.findtext("value")
        if not key or key in values or not value or not value.strip():
            raise ValueError(f"Clave vacía, duplicada o texto vacío: {path.name}/{key}")
        # Las cadenas de esta fase usan solo {n}; formatos futuros requieren ampliar esta comprobación.
        remainder = re.sub(r"\{\d+\}", "", value.replace("{{", "").replace("}}", ""))
        if "{" in remainder or "}" in remainder:
            raise ValueError(f"Formato no admitido o llaves incorrectas: {path.name}/{key}")
        values[key] = value
    return values


def main():
    directory = ROOT / "PalClam" / "Localization"
    maps = {lang: read_resources(directory / ("Strings" + ("" if lang == "es" else "." + lang) + ".resx"))
            for lang in LANGUAGES}
    spanish = maps["es"]
    assert len(spanish) >= 30, "Cobertura inicial incompleta"
    for language, values in maps.items():
        assert values.keys() == spanish.keys(), f"Claves diferentes: {language}"
        for key, text in values.items():
            assert sorted(re.findall(r"\{(\d+)\}", text)) == sorted(re.findall(r"\{(\d+)\}", spanish[key])), f"Parámetros: {language}/{key}"
            assert not text.startswith("⟦"), f"Marcador en lugar de traducción: {language}/{key}"
    used = set()
    for path in (ROOT / "PalClam").rglob("*.cs"):
        used.update(re.findall(r'"((?:Header|Options|Help|Language)\.[A-Za-z.]+)"', path.read_text(encoding="utf-8-sig")))
    assert used == {key for key in spanish if not key.startswith('Display.')}, f"Claves sin enlazar o ausentes: {used ^ {key for key in spanish if not key.startswith('Display.')}}"
    assert len(re.findall(r'new UiLanguage\("', (directory / "TextCatalog.cs").read_text())) == 6
    project = ET.parse(ROOT / "PalClam/PalClam.csproj").getroot()
    assert project.findtext("PropertyGroup/Version") == "1.2.2"
    assert project.findtext("PropertyGroup/NeutralLanguage") == "es"
    iss = (ROOT / "Installer/PalClam.iss").read_text(encoding="utf-8-sig")
    assert "AppId={{C74FA674-9A6D-471B-B1D0-195437635D29}" in iss
    assert "OutputBaseFilename={#SetupBaseName}" in iss
    publication = (ROOT / "Tools/PublicacionBloqueada.ps1").read_text()
    assert "[BLOQUEADO]" in publication and "exit 1" in publication
    acta = (ROOT / "docs/ACEPTACION_1.2.2.ini").read_text()
    checks = [line.split("=", 1) for line in acta.splitlines() if line and not line.startswith("#")]
    assert len(dict(checks)) == len(checks), "Duplicados en acta"
    assert dict(checks).pop("VERSION") == "1.2.2"
    assert all(value == "PENDIENTE" for key, value in checks if key != "VERSION"), "Aceptación declarada sin evidencia"
    for path in ROOT.rglob("*.csproj"):
        tree = ET.parse(path)
        for node in tree.findall(".//Compile"):
            target = node.get("Include", "")
            if target and "*" not in target:
                assert (path.parent / target.replace("\\", "/")).is_file(), f"Enlace de código roto: {path}/{target}"
    print(f"[OK ESTÁTICO] {len(spanish)} claves × 6 idiomas = {sum(map(len, maps.values()))} textos; enlaces, formatos, versión, AppId y bloqueo.")
    print("Esta comprobación es estática. Los resultados de .NET y los ensayos Windows se documentan por separado.")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        sys.exit(1)
