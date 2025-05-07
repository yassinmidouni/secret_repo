#!/usr/bin/env python3
import base64
import sys

# XOR key defined in Stage 1
xor_key = bytes([0x13, 0x37, 0x42, 0x69, 0x72, 0x64])

# Read the Stage 2 executable
if len(sys.argv) != 2:
    print("Usage: python encode_stage2.py <path_to_stage2_exe>")
    sys.exit(1)

input_file = sys.argv[1]
print(f"Encoding file: {input_file}")

# Read the Stage 2 executable
with open(input_file, "rb") as f:
    data = f.read()

print(f"Original file size: {len(data)} bytes")

# XOR encrypt
encrypted = bytearray()
for i in range(len(data)):
    encrypted.append(data[i] ^ xor_key[i % len(xor_key)])

print(f"XOR encryption applied with key: {xor_key.hex()}")

# Base64 encode
encoded = base64.b64encode(encrypted).decode('utf-8')

print(f"Base64 encoded size: {len(encoded)} characters")

# Write to file
output_file = "stage2.txt"
with open(output_file, "w") as f:
    f.write(encoded)

print(f"Encoded file written to: {output_file}")
print("Done!")
