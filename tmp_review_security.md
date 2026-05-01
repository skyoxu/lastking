## Task 49 瀹夊叏璇箟瀹¤锛堝熀浜庣‘瀹氭€ц瘉鎹級

### P1
1. [`.taskmaster/tasks/tasks_back.json`](F:\Lastking\.taskmaster\tasks\tasks_back.json), [`.taskmaster/tasks/tasks_gameplay.json`](F:\Lastking\.taskmaster\tasks\tasks_gameplay.json)  
   `acceptance` 鏂板浜嗏€渁tomic deployment / partial state failure鈥濊涔夛紙鏈€鍚庝竴鏉★級锛屼絾褰撳墠寮曠敤娴嬭瘯闆嗕腑娌℃湁鐩存帴鏂█鈥滃け璐ユ椂璧勬簮/闃熷垪鏄惁蹇呴』鍥炴粴鎴栬繘鍏ユ樉寮?failure state鈥濄€? 
   鐜板湪 `test_deployment_with_missing_owner_or_faction_is_not_counted_as_valid_battle_loop_registration` 鍙獙璇佷簡 `completed_units=[]`銆乣failed_deployments=["spearman"]`銆佹棤 active unit锛涘悓鏃堕槦鍒楀凡瀹屾垚涓旇祫婧愬凡鎵ｅ噺锛?20/115锛夈€傝繖涓庘€渁tomic: all-or-failure鈥濊〃杩板瓨鍦ㄨ涔夊紶鍔涖€? 
   瑕佹敼浠€涔堬細  
   - 瑕佷箞鏀舵暃 acceptance 鏂囨锛屾槑纭€渇ailure鈥濆畾涔変负鈥滀笉娉ㄥ唽鎴樺満鍗曚綅涓斿彂鍑?failed_deployments鈥濓紝骞跺０鏄庤祫婧愪笉鍥炴粴鏄棦瀹氬绾︼紱  
   - 瑕佷箞琛ュ厖娴嬭瘯锛屾柇瑷€澶辫触鍒嗘敮鐨勫師瀛愯ˉ鍋胯涔夛紙渚嬪鍥炴粴璧勬簮涓庨槦鍒楋級骞跺疄鐜板搴旇涓恒€? 

### P2
1. [`Tests.Godot/tests/Integration/test_barracks_training_queue_flow.gd`](F:\Lastking\Tests.Godot\tests\Integration\test_barracks_training_queue_flow.gd)  
   `ACC:T49.5`锛圚UD/feedback from completion+deployment锛変富瑕佹寕鍦?`test_training_queue_signals_should_reflect_enqueue_cancel_complete_changes`锛屼絾璇ユ祴璇曟柇瑷€鐨勬槸 bridge signal 鏀堕泦锛屼笉鏄?HUD 鍦烘櫙灞傚弽棣堝憟鐜般€? 
   瑕佹敼浠€涔堬細  
   - 鍦ㄥ搴?HUD 娴嬭瘯鏂囦欢琛ヤ竴涓?T49 閿氱偣娴嬭瘯锛堣闃?QueueCompleted 鎴栧叾妗ユ帴浜嬩欢鍚庯紝鏂█ UI 鏂囨湰/鐘舵€佸彉鍖栵級锛屾垨璋冩暣 acceptance 鐨?`Refs:` 鍘绘帀 HUD 寮鸿涔夈€? 

2. [`Game.Core.Tests/State/TechRuntimeSnapshotTests.cs`](F:\Lastking\Game.Core.Tests\State\TechRuntimeSnapshotTests.cs)  
   `ACC:T49.9` 閿氱偣鐩墠鎸傚湪 鈥渟napshot change/immutability鈥濇祴璇曪紝鍜屸€滈儴缃插師瀛愭€уけ璐?partial state鈥濆苟闈炲悓涓€璇箟灞傘€? 
   瑕佹敼浠€涔堬細  
   - 灏?`ACC:T49.9` 杩佺Щ鍒扮湡姝ｈ鐩栤€滈儴缃插け璐ュ悗鐘舵€佷竴鑷存€р€濈殑娴嬭瘯锛屾垨鏂板 Core/Godot 渚уけ璐ヤ竴鑷存€ф祴璇曞苟閲嶆寕閿氱偣銆? 

### Deterministic gates 缁撹
- `sc-test`: `ok`锛堝惈 xUnit + GdUnit + smoke锛? 
- `sc-acceptance-check`: `ok`  
- 浣?`sc-llm-review` 鍏冩暟鎹樉绀?`acceptance_meta.status = "missing"`锛堝綋娆″璁¤緭鍏ョ己澶?acceptance semantic section锛夛紝璇ユ Needs Fix 涓嶈兘鐩存帴浣滀负浠ｇ爜缂洪櫡璇佹嵁銆? 
- 鏈缁撹浠呭熀浜庡綋鍓?diff + 娴嬭瘯鍐呭鍙獙璇佽涔夈€?

### 寮辨祴璇曟彁绀?
- 鏈彂鐜扳€滀粎鏈?anchor 鏃犳柇瑷€鈥濈殑鏄庢樉绌哄３娴嬭瘯銆? 
- 涓昏闂鏄€滄柇瑷€璇箟涓?acceptance 鏂囨寮哄害涓嶅畬鍏ㄥ榻愨€濓紙灏ゅ叾 atomic failure 涓?HUD 璇箟閾捐矾锛夈€?

Verdict: Needs Fix
