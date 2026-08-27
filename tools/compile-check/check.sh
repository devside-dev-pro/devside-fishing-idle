#!/usr/bin/env bash
# Vérification de compilation HORS Unity (sessions distantes, CI légère).
#
# Unity refuse de lancer quoi que ce soit tant qu'une erreur de compilation
# traîne — et sans éditeur sous la main, ces erreurs se découvraient chez le
# testeur. Ce script compile tout le projet contre des stubs d'UnityEngine et
# de NUnit : il n'exécute rien, il ne remplace pas le Test Runner, mais il
# attrape les fautes de frappe, les membres inexistants et les erreurs de type.
#
# Prérequis : mono-mcs (apt-get install -y mono-mcs)
# Usage : tools/compile-check/check.sh
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
out="$(mktemp -d)/check.dll"

mcs -langversion:latest -target:library -out:"$out" \
    "$root"/Assets/Scripts/Core/*.cs \
    "$root"/Assets/Scripts/Game/*.cs \
    "$root"/Assets/Tests/EditMode/*.cs \
    "$root"/tools/compile-check/UnityStub.cs \
    "$root"/tools/compile-check/NUnitStub.cs

echo "✅ Compilation OK (Core + Game + tests)."
