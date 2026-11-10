"""Serve the Addressables ServerData directory for the local update demo."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class ContentHandler(SimpleHTTPRequestHandler):
    def send_head(self):
        if not self.path.split("?", 1)[0].lower().endswith(".bundle"):
            # A second build can change a hash within the same filesystem timestamp second.
            # Always return current catalog bytes instead of a timestamp-based 304 response.
            if "If-Modified-Since" in self.headers:
                del self.headers["If-Modified-Since"]
        return super().send_head()

    def end_headers(self):
        path = self.path.split("?", 1)[0].lower()
        # Catalogs and hashes must be revalidated; hashed bundle names are immutable.
        self.send_header("Cache-Control", "public, max-age=31536000, immutable" if path.endswith(".bundle") else "no-cache")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent / "ServerData")
    parser.add_argument("--bind", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8080)
    args = parser.parse_args()
    root = args.root.resolve()
    if not root.is_dir():
        parser.error(f"Content directory not found: {root}. Build baseline resources in Unity first.")
    handler = partial(ContentHandler, directory=str(root))
    with ThreadingHTTPServer((args.bind, args.port), handler) as server:
        print(f"Serving {root} at http://{args.bind}:{args.port}/", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
