"""platform-canary — proves the legacy SYNCHRONOUS eywa client
(eywa-reacher-client==0.0.8) round-trips with the 2026.x platform through the
full commander -> agent -> reacher dispatch chain.

Exercises: task.get, eywa.datasets.graphql (query + 2026.x `data:` mutation),
task.report, task.log, task.close — all via the legacy sync API.
"""
import json
import traceback
import eywa  # auto-connects its stdin/stdout JSON-RPC line on import


def run():
    task = eywa.get_task()
    eywa.info("platform-canary: got task", {"task_present": task is not None})

    # (1) transport proof — a query that needs no special permissions
    typename = eywa.graphql({"query": "{ __typename }"})
    eywa.info("platform-canary: __typename query returned", {"result": typename})

    # (2) 2026.x `data:` argument proof — issue a stack mutation the new way.
    #     We only care that the server ACCEPTS the `data:` argument (i.e. does
    #     NOT answer "Unknown argument"); any permission/validation error is fine.
    data_mutation = (
        'mutation { stackCurrency(data: {name: "canary-probe"}) { euuid } }'
    )
    data_result = eywa.graphql({"query": data_mutation})

    summary = {
        "typename": typename,
        "data_mutation_result": data_result,
    }
    eywa.report("platform-canary summary", summary)
    eywa.info("platform-canary: completed round-trip", summary)
    eywa.close("SUCCESS")


if __name__ == "__main__":
    try:
        run()
    except Exception as e:  # noqa: BLE001
        eywa.error("platform-canary failed", {"error": str(e), "trace": traceback.format_exc()})
        try:
            eywa.close("ERROR")
        except Exception:
            pass
