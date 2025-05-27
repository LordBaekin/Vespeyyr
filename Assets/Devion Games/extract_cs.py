#!/usr/bin/env python3
"""
extract_cs.py

Recursively find all .cs and .asmdef files under the current directory (or a given one),
and pack them into a zip archive, preserving their relative paths.
"""

import os
import argparse
import zipfile
import sys

def zip_source_files(src_dir: str, zip_path: str) -> None:
    src_dir = os.path.abspath(src_dir)
    if not os.path.isdir(src_dir):
        print(f"Error: source directory '{src_dir}' does not exist.", file=sys.stderr)
        sys.exit(1)

    # Make sure output dir exists
    out_dir = os.path.dirname(os.path.abspath(zip_path))
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir, exist_ok=True)

    included_extensions = {'.cs', '.asmdef'}

    with zipfile.ZipFile(zip_path, mode='w', compression=zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(src_dir):
            for fname in files:
                if any(fname.lower().endswith(ext) for ext in included_extensions):
                    abs_path = os.path.join(root, fname)
                    rel_path = os.path.relpath(abs_path, src_dir)
                    zipf.write(abs_path, arcname=rel_path)
                    print(f"Added: {rel_path}")
    print(f"\n✓ Created ZIP at: {zip_path}")

def main():
    parser = argparse.ArgumentParser(
        description="Zip all .cs and .asmdef files from the current (or a specified) directory."
    )
    parser.add_argument(
        '-s', '--source',
        default='.',
        help="Source directory (defaults to current directory)"
    )
    parser.add_argument(
        '-o', '--output',
        default='cs_files.zip',
        help="Output .zip file name (defaults to cs_files.zip)"
    )
    args = parser.parse_args()

    zip_source_files(args.source, args.output)

if __name__ == "__main__":
    main()
