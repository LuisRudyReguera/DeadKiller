import json
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

import claude_bridge


class BridgeTests(unittest.TestCase):
    def test_missing_cli(self):
        with patch.object(claude_bridge, "executable", return_value=None):
            self.assertFalse(json.loads(claude_bridge.status({}))["available"])
            with self.assertRaises(RuntimeError):
                claude_bridge.review({"question": "Review", "context": "Code"})

    def test_limits(self):
        for args in ({}, {"question": "x", "context": "x" * 18001},
                     {"question": 1, "context": "x"}):
            with self.assertRaises(ValueError):
                claude_bridge.review(args)

    @patch.object(claude_bridge, "executable", return_value="claude.exe")
    @patch.object(claude_bridge.subprocess, "run")
    def test_review_is_isolated(self, run, _executable):
        run.return_value = subprocess.CompletedProcess([], 0, json.dumps({"result": "Review", "usage": {}}))
        answer = json.loads(claude_bridge.review({"question": "Check", "context": "Untrusted code"}))
        self.assertEqual(answer["review"], "Review")
        args, kwargs = run.call_args
        command = args[0]
        self.assertEqual(command[command.index("--tools") + 1], "")
        self.assertEqual(command[command.index("--mcp-config") + 1], '{"mcpServers":{}}')
        self.assertFalse(kwargs["shell"])
        self.assertNotEqual(Path(kwargs["cwd"]), Path.cwd())
        self.assertEqual(kwargs["timeout"], 120)

    @patch.object(claude_bridge, "executable", return_value="claude.exe")
    @patch.object(claude_bridge.subprocess, "run")
    def test_cli_error_does_not_leak_output(self, run, _executable):
        run.return_value = subprocess.CompletedProcess([], 1, "", "private diagnostics")
        with self.assertRaisesRegex(RuntimeError, "inicio de sesion"):
            claude_bridge.review({"question": "Check", "context": "Code"})

    def test_stdio_protocol(self):
        messages = [
            {"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {}},
            {"jsonrpc": "2.0", "id": 2, "method": "tools/list"},
            {"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "read_section", "arguments": {"document": "ROADMAP", "heading": "Estado actual"}}},
            {"jsonrpc": "2.0", "id": 4, "method": "tools/call", "params": {"name": "claude_review", "arguments": {}}},
        ]
        result = subprocess.run([sys.executable, str(Path(__file__).with_name("server.py"))],
                                input="\n".join(map(json.dumps, messages)) + "\n",
                                text=True, encoding="utf-8", capture_output=True, timeout=10)
        self.assertEqual(result.returncode, 0, result.stderr)
        replies = [json.loads(line) for line in result.stdout.splitlines()]
        self.assertEqual(len(replies), 4)
        self.assertIn("claude_review", [t["name"] for t in replies[1]["result"]["tools"]])
        self.assertIn("Hito activo", replies[2]["result"]["content"][0]["text"])
        self.assertTrue(replies[3]["result"]["isError"])


if __name__ == "__main__":
    unittest.main()
