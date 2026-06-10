"""platform-canary — proves the legacy SYNCHRONOUS eywa client
(eywa-reacher-client==0.0.8) round-trips with the 2026.x platform through the
full commander -> agent -> reacher dispatch chain.

Exercises: task.get, eywa.datasets.graphql (query + the 2026.x `data:` arg vs the
old entity-named arg), task.report, task.log, task.close — all via the legacy
sync API. The task succeeds once the transport round-trip is proven; the
mutation probes are best-effort and only reported.
"""
import traceback
import eywa  # auto-connects its stdin/stdout JSON-RPC line on import


def probe(query):
    """Run a graphql call, return {'ok':..., 'result'|'error':...} without raising."""
    try:
        return {"ok": True, "result": eywa.graphql({"query": query})}
    except Exception as e:  # noqa: BLE001
        return {"ok": False, "error": str(e)}


def run():
    task = eywa.get_task()
    eywa.info("platform-canary: got task", {"task_present": task is not None})

    # (1) transport proof — a permission-free query
    typename = eywa.graphql({"query": "{ __typename }"})
    eywa.info("platform-canary: __typename query returned", {"result": typename})

    # (2) prove the 2026.x rename: OLD entity-named arg should be rejected,
    #     NEW `data:` arg should be accepted (parsed) by the server.
    old_style = probe('mutation { stackBurton(burton: {}) { euuid } }')
    new_style = probe('mutation { stackBurton(data: {}) { euuid } }')
    eywa.info("platform-canary: old-arg probe", old_style)
    eywa.info("platform-canary: data-arg probe", new_style)

    summary = {"typename": typename, "old_arg": old_style, "data_arg": new_style}
    eywa.report("platform-canary summary", summary)
    eywa.info("platform-canary: round-trip complete", summary)
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
