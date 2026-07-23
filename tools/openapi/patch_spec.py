#!/usr/bin/env python3
"""Overlay/preprocess for TheTVDB v4 OpenAPI spec, applied before NSwag codegen.

TheTVDB's swagger types every integer resource id as ``schema: {type: number}``,
which NSwag maps to C# ``double`` — so generated methods take ``double id`` and
entities would expose ``double`` ids. This overlay coerces those *path* id
parameters to ``integer``/``int64`` so they generate as ``long``.

Scope is deliberately narrow (path parameters only): the only other ``number``
fields in the spec are the ``score`` properties, which are genuinely
floating-point and must stay ``double``.

Idempotent — safe to re-run. Usage:  patch_spec.py <input.yml> <output.yml>
"""
import sys
import yaml


def coerce_path_id_params(spec: dict) -> list[str]:
    """Coerce every `in: path` parameter typed `number` to integer/int64.

    Returns a list of "METHOD /path :: paramName" for each parameter changed.
    """
    changed: list[str] = []
    for path, item in (spec.get("paths") or {}).items():
        if not isinstance(item, dict):
            continue
        for method, op in item.items():
            if not isinstance(op, dict) or "parameters" not in op:
                continue
            for param in op.get("parameters") or []:
                if param.get("in") != "path":
                    continue
                schema = param.get("schema") or {}
                if schema.get("type") == "number":
                    schema["type"] = "integer"
                    schema["format"] = "int64"
                    param["schema"] = schema
                    changed.append(f"{method.upper()} {path} :: {param.get('name')}")
    return changed


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: patch_spec.py <input.yml> <output.yml>", file=sys.stderr)
        return 2
    src, dst = sys.argv[1], sys.argv[2]
    with open(src) as f:
        spec = yaml.safe_load(f)

    changed = coerce_path_id_params(spec)

    with open(dst, "w") as f:
        yaml.safe_dump(spec, f, sort_keys=False, allow_unicode=True, width=4096)

    print(f"overlay: coerced {len(changed)} path id param(s) number → int64")
    for c in changed:
        print(f"   {c}")
    print(f"overlay: wrote {dst}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
