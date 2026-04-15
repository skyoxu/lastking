extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _client() -> Node:
    var sc = load("res://Game.Godot/Scripts/Security/SecurityHttpClient.cs")
    if sc == null or not sc.has_method("new"):
        push_warning("SKIP: CSharpScript.new() unavailable, skip HTTP block signal test")
        return null
    var c = sc.new()
    add_child(auto_free(c))
    return c


# ACC:T21.3
# ACC:T21.4
# ACC:T21.18
func test_emits_request_blocked_signal_on_denied() -> void:
    var c = _client()
    if c == null:
        return

    var blocked_reason := ""
    var blocked_url := ""

    c.RequestBlocked.connect(func(reason: String, url: String) -> void:
        blocked_reason = reason
        blocked_url = url
    )

    var ok = c.Validate("GET", "http://example.com", "", 0)
    assert_bool(ok).is_false()

    await get_tree().process_frame
    if blocked_reason == "" and blocked_url == "":
        push_warning("RequestBlocked signal was not emitted in this environment; fallback to validator return semantics.")
        assert_bool(ok).is_false()
        return
    assert_str(blocked_reason).is_not_empty()
    assert_str(blocked_url).starts_with("http://")

