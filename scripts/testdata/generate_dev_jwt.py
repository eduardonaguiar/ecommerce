#!/usr/bin/env python3
"""Generate an HS256 JWT compatible with local gateway defaults."""

from __future__ import annotations

import argparse
import base64
import hashlib
import hmac
import json
import time


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("ascii")


def encode_part(payload: dict) -> str:
    return b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))


def generate_token(secret: str, issuer: str, audience: str, subject: str, ttl_seconds: int) -> str:
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    payload = {
        "iss": issuer,
        "aud": audience,
        "sub": subject,
        "iat": now,
        "exp": now + ttl_seconds,
    }

    signing_input = f"{encode_part(header)}.{encode_part(payload)}"
    signature = hmac.new(secret.encode("utf-8"), signing_input.encode("utf-8"), hashlib.sha256).digest()
    return f"{signing_input}.{b64url(signature)}"


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate a local dev JWT (HS256).")
    parser.add_argument("--secret", required=True, help="HMAC secret used to sign the token.")
    parser.add_argument("--issuer", default="https://auth.local", help="Token issuer (iss).")
    parser.add_argument("--audience", default="ecommerce-api", help="Token audience (aud).")
    parser.add_argument("--subject", default="dev-local-user", help="Token subject (sub).")
    parser.add_argument("--ttl-seconds", type=int, default=3600, help="Token lifetime in seconds.")
    args = parser.parse_args()

    print(
        generate_token(
            secret=args.secret,
            issuer=args.issuer,
            audience=args.audience,
            subject=args.subject,
            ttl_seconds=args.ttl_seconds,
        )
    )


if __name__ == "__main__":
    main()
