#!/usr/bin/env python3
import base64
import sys

def encode_url_parts(url):
    # Split URL into smaller chunks (~4 characters each)
    chunks = []
    for i in range(0, len(url), 4):
        chunks.append(url[i:i+4])
    
    # Base64 encode each chunk
    encoded_chunks = []
    for chunk in chunks:
        encoded = base64.b64encode(chunk.encode('utf-8')).decode('utf-8')
        encoded_chunks.append(f'"{encoded}"')
    
    # Format for C# array
    result = ", ".join(encoded_chunks)
    return result

if len(sys.argv) != 2:
    print("Usage: python url_encoder.py <url>")
    sys.exit(1)

url = sys.argv[1]
print("\nOriginal URL:", url)

# Split into server and path parts
if "://" in url:
    protocol_idx = url.index("://")
    domain_end = url.find("/", protocol_idx + 3)
    
    if domain_end > 0:
        server_part = url[:domain_end]
        path_part = url[domain_end:]
    else:
        server_part = url
        path_part = "/"
else:
    server_part = ""
    path_part = url

print("Server part:", server_part)
print("Path part:", path_part)

# Encode both parts
print("\nEncoded server parts:")
print("private static readonly string[] serverParts = new string[] {")
print("    " + encode_url_parts(server_part))
print("};")

print("\nEncoded path parts:")
print("private static readonly string[] pathParts = new string[] {")
print("    " + encode_url_parts(path_part))
print("};")
