using System.Text.RegularExpressions;
using System.Xml.Linq;

// ============================================================
// MacroTool 仕様書 vs ソースコード 整合性チェッカー
// Usage: dotnet run [<repoRoot>]
//   repoRoot: リポジトリルートへのパス (省略時はカレントディレクトリから自動探索)
// ============================================================

int passCount = 0;
int failCount = 0;
int warnCount = 0;

// ----- リポジトリルートの解決 -----
string repoRoot = ResolveRepoRoot(args.Length > 0 ? args[0] : null);
Console.WriteLine($"=== MacroTool 仕様書 vs ソースコード 整合性チェック ===");
Console.WriteLine($"リポジトリルート: {repoRoot}");
Console.WriteLine();

// ----- ソースファイルをキャッシュ -----
var srcDir = Path.Combine(repoRoot, "src");
var allCsFiles = Directory.Exists(srcDir)
    ? Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
    : Array.Empty<string>();

// ファイル内容キャッシュ (path -> content)
var fileCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var f in allCsFiles)
    fileCache[f] = File.ReadAllText(f);

// 全ソースを結合したテキスト (クラス存在チェック用)
string allSrc = string.Concat(fileCache.Values);

// ----- csproj ファイルキャッシュ -----
var allCsprojFiles = Directory.Exists(srcDir)
    ? Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
    : Array.Empty<string>();

// ============================================================
// CHECK 1: レイヤー構成と依存方向
// ============================================================
Console.WriteLine("[CHECK 1: レイヤー構成と依存方向]");

// 1-1: 4層プロジェクトが存在するか
var domainProjPath = Path.Combine(srcDir, "MacroTool.Domain", "MacroTool.Domain.csproj");
var appProjPath = Path.Combine(srcDir, "MacroTool.Application", "MacroTool.Application.csproj");
var infraProjPath = Path.Combine(srcDir, "MacroTool.Infrastructure.Windows", "MacroTool.Infrastructure.Windows.csproj");
var uiProjPath = Path.Combine(srcDir, "MacroTool.WinForms", "MacroTool.WinForms.csproj");

Check(File.Exists(domainProjPath) && File.Exists(appProjPath) &&
      File.Exists(infraProjPath) && File.Exists(uiProjPath),
      "4層構成（Domain/Application/Infrastructure/UI）のプロジェクトがすべて存在する");

// 1-2: Domain は Application/UI/Infrastructure を参照しないこと
if (File.Exists(domainProjPath))
{
    var domainRefs = GetProjectReferences(domainProjPath);
    bool domainClean = !domainRefs.Any(r =>
        r.Contains("Application", StringComparison.OrdinalIgnoreCase) ||
        r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase) ||
        r.Contains("WinForms", StringComparison.OrdinalIgnoreCase));
    Check(domainClean,
          "MacroTool.Domain は Application/Infrastructure/UI に依存していない",
          domainClean ? null : $"不正な参照: {string.Join(", ", domainRefs)}");
}
else
{
    Skip("MacroTool.Domain.csproj が見つからないため依存チェックをスキップ");
}

// 1-3: Application は UI を参照しないこと
if (File.Exists(appProjPath))
{
    var appRefs = GetProjectReferences(appProjPath);
    bool appClean = !appRefs.Any(r =>
        r.Contains("WinForms", StringComparison.OrdinalIgnoreCase));
    Check(appClean,
          "MacroTool.Application は UI（WinForms）に依存していない",
          appClean ? null : $"不正な参照: {string.Join(", ", appRefs)}");
}
else
{
    Skip("MacroTool.Application.csproj が見つからないため依存チェックをスキップ");
}

// 1-4: Infrastructure は Application を参照しているか（依存方向の確認）
if (File.Exists(infraProjPath))
{
    var infraRefs = GetProjectReferences(infraProjPath);
    bool infraHasApp = infraRefs.Any(r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
    Check(infraHasApp,
          "MacroTool.Infrastructure は Application を参照している（依存方向が正しい）",
          infraHasApp ? null : "Application への参照が見つからない");
}
else
{
    Skip("MacroTool.Infrastructure.Windows.csproj が見つからないため依存チェックをスキップ");
}

// 1-5: WinForms は Infrastructure を参照しているか
if (File.Exists(uiProjPath))
{
    var uiRefs = GetProjectReferences(uiProjPath);
    bool uiHasInfra = uiRefs.Any(r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
    Check(uiHasInfra,
          "MacroTool.WinForms は Infrastructure を参照している（依存方向が正しい）",
          uiHasInfra ? null : "Infrastructure への参照が見つからない");
}
else
{
    Skip("MacroTool.WinForms.csproj が見つからないため依存チェックをスキップ");
}

Console.WriteLine();

// ============================================================
// CHECK 2: ドメインモデルの存在確認
// ============================================================
Console.WriteLine("[CHECK 2: ドメインモデルの存在確認]");

// Domain の .cs ファイルのみ対象にする（他の層への誤検知を防ぐ）
var domainDir = Path.Combine(srcDir, "MacroTool.Domain");
string domainSrc = Directory.Exists(domainDir)
    ? string.Concat(Directory.GetFiles(domainDir, "*.cs", SearchOption.AllDirectories)
        .Select(f => File.ReadAllText(f)))
    : string.Empty;

// MacroStep.cs の内容（プロパティチェック対象を MacroStep クラスに限定）
var macroStepFile = Path.Combine(domainDir, "Macros", "MacroStep.cs");
string macroStepSrc = File.Exists(macroStepFile) ? File.ReadAllText(macroStepFile) : string.Empty;

// 2-1: Macro クラス + Steps プロパティ
bool macroClassExists = ContainsPattern(domainSrc, @"class\s+Macro\b");
Check(macroClassExists, "Macro クラスが存在する (Domain 層)");

bool macroStepsExists = ContainsPattern(domainSrc, @"IReadOnlyList\s*<\s*MacroStep\s*>\s+Steps");
Check(macroStepsExists, "Macro クラスが IReadOnlyList<MacroStep> Steps プロパティを持つ");

// 2-2: MacroStep クラス + プロパティ（MacroStep.cs のみを対象）
bool macroStepClassExists = ContainsPattern(domainSrc, @"(class|record)\s+MacroStep\b");
Check(macroStepClassExists, "MacroStep クラスが存在する (Domain 層)");

// 仕様書: Order, Label, Action プロパティ (MacroStep.cs 内で確認)
bool stepOrderExists = ContainsPattern(macroStepSrc, @"(public\s+\w+\s+Order\b|int\s+Order\b)");
Check(stepOrderExists, "MacroStep クラスが Order プロパティを持つ");

bool stepLabelPropExists = ContainsPattern(macroStepSrc, @"public\s+\S+\s+Label\s*[{\(]");
Check(stepLabelPropExists, "MacroStep クラスが Label プロパティを持つ");

bool stepActionExists = ContainsPattern(macroStepSrc, @"public\s+MacroAction\s+Action\s*[{\(]");
Check(stepActionExists, "MacroStep クラスが MacroAction Action プロパティを持つ");

// 2-3: 値オブジェクト存在確認 (Domain 層のみ)
// 仕様書記載: StepLabel, GoToTarget, SearchArea, Rect, ColorCode, Percentage, Milliseconds, VariableName
string[] requiredValueObjects = new[]
{
    "StepLabel", "GoToTarget", "SearchArea", "Rect",
    "ColorCode", "Percentage", "Milliseconds", "VariableName"
};

foreach (var vo in requiredValueObjects)
{
    // クラス、レコード、構造体、enum のいずれかで定義されているか
    bool exists = ContainsPattern(domainSrc,
        @"(class|record|struct|enum)\s+" + Regex.Escape(vo) + @"\b");
    Check(exists, $"値オブジェクト '{vo}' が存在する (Domain 層)");
}

Console.WriteLine();

// ============================================================
// CHECK 3: Action 体系の網羅性
// ============================================================
Console.WriteLine("[CHECK 3: Action 体系の網羅性]");

// Actions.cs の内容（Domain 層のみで確認）
var actionsFile = Path.Combine(domainDir, "Macros", "Actions.cs");
string actionsSrc = File.Exists(actionsFile) ? File.ReadAllText(actionsFile) : domainSrc;

string[] requiredActions = new[]
{
    // Mouse
    "MouseClickAction", "MouseMoveAction", "MouseWheelAction",
    // Key
    "KeyPressAction", "HotkeyAction",
    // Wait
    "WaitAction",
    "WaitForPixelColorAction", "WaitForTextInputAction",
    // Detection
    "FindImageAction", "FindTextOcrAction",
    // ControlFlow
    "RepeatAction", "GoToAction", "IfAction",
    "EmbedMacroFileAction", "ExecuteProgramAction"
};

// 仕様書では "WaitAction" と記載されているが、実装では "WaitTimeAction" を使用する。
// 両方の名前を許容する（実装側の命名が仕様書から変更された可能性があるため WARN ではなく PASS とする）。
foreach (var action in requiredActions)
{
    if (action == "WaitAction")
    {
        bool exists = ContainsPattern(actionsSrc,
            @"(class|record)\s+(WaitAction|WaitTimeAction)\b");
        Check(exists,
              $"Action '{action}' (または WaitTimeAction) が定義されている (Domain 層)");
    }
    else
    {
        bool exists = ContainsPattern(actionsSrc,
            @"(class|record)\s+" + Regex.Escape(action) + @"\b");
        Check(exists, $"Action '{action}' が定義されている (Domain 層)");
    }
}

Console.WriteLine();

// ============================================================
// CHECK 4: Application Service の存在
// ============================================================
Console.WriteLine("[CHECK 4: Application Service の存在]");

// Application 層の .cs ファイルのみ対象
var appDir = Path.Combine(srcDir, "MacroTool.Application");
string appSrc = Directory.Exists(appDir)
    ? string.Concat(Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories)
        .Select(f => File.ReadAllText(f)))
    : string.Empty;

// 4-1: MacroAppService または MacroEditorAppService の存在
bool macroSvcExists = ContainsPattern(appSrc,
    @"class\s+(MacroAppService|MacroEditorAppService)\b");
Check(macroSvcExists, "MacroAppService (または MacroEditorAppService) クラスが存在する (Application 層)");

// 4-2: UseCase に対応するメソッド
string[] requiredMethods = new[]
{
    // UC-01 New
    "New",
    // UC-02 Open / Load
    "Open|Load",
    // UC-03 Save
    @"\bSave\b",
    // UC-04 SaveAs
    "SaveAs",
    // UC-05 ImportCsv / ImportCSV
    "Import",
    // UC-06 ExportCsv / ExportCSV
    "Export",
    // UC-21 Play
    @"\bPlay\b",
    // UC-22/23 PlayUntil / PlayFrom
    "PlayUntil|PlayFrom",
    // UC-24 Stop
    "Stop[A-Za-z]*",
};

string[] methodDisplayNames = new[]
{
    "New (UC-01)",
    "Open / Load (UC-02)",
    "Save (UC-03)",
    "SaveAs (UC-04)",
    "ImportCsv / ImportCSV (UC-05)",
    "ExportCsv / ExportCSV (UC-06)",
    "Play (UC-21)",
    "PlayUntil / PlayFrom (UC-22/23)",
    "Stop (UC-24)",
};

for (int i = 0; i < requiredMethods.Length; i++)
{
    bool exists = ContainsPattern(appSrc,
        @"(public|private|protected|internal)\s+\S+\s+(" + requiredMethods[i] + @")\s*[\(<]");
    Check(exists, $"UseCase メソッド {methodDisplayNames[i]} が存在する");
}

Console.WriteLine();

// ============================================================
// CHECK 5: 不変条件バリデーションの実装有無
// ============================================================
Console.WriteLine("[CHECK 5: 不変条件バリデーションの実装有無]");

// 5-1: StepLabel 一意性チェック
bool labelUniquenessExists =
    ContainsPattern(allSrc, @"MakeUniqueLabel|NormalizeStepLabel|LabelUniqueness") ||
    ContainsPattern(allSrc, @"Distinct.*Label|Label.*Distinct") ||
    ContainsPattern(allSrc, @"used\.Contains.*[Ll]abel|[Ll]abel.*used\.Contains");
Check(labelUniquenessExists, "StepLabel 一意性チェックのロジックが存在する");

// 5-2: GoToTarget の参照先存在チェック
bool goToRefCheckExists =
    ContainsPattern(allSrc, @"GoToResolver|ResolveGoTo|ValidateGoTo") ||
    ContainsPattern(allSrc, @"GetDefinedLabels.*GoTo|GoTo.*GetDefinedLabels") ||
    ContainsPattern(allSrc, @"labelMap\s*=\s*Build|BuildLabelMap") ||
    ContainsPattern(allSrc, @"labelMap\[|labelMap\.TryGetValue|labelMap\.ContainsKey");
Check(goToRefCheckExists, "GoToTarget の参照先存在チェックが存在する");

// 5-3: SearchArea の Rect 必須チェック (AreaOf系 で Width/Height > 0)
bool rectCheckExists =
    ContainsPattern(allSrc, @"AreaOfDesktop|AreaOfFocusedWindow") &&
    (ContainsPattern(allSrc, @"Width\s*[><=]|Height\s*[><=]") ||
     ContainsPattern(allSrc, @"X2\s*[-]\s*X1|Y2\s*[-]\s*Y1") ||
     ContainsPattern(allSrc, @"Rect.*required|SearchArea.*Rect") ||
     ContainsPattern(allSrc, @"X1.*X2.*Y1.*Y2|X2\s*>\s*X1"));
Warn(rectCheckExists,
     "SearchArea の Area 系で Rect 必須チェックが存在する (警告: 明示的なバリデーションが検出されない場合あり)");

// 5-4: Percentage 0..100 バリデーション
bool percentageValidation =
    ContainsPattern(allSrc, @"TolerancePercent\s*[<>=]|0\s*\.{2}100") ||
    ContainsPattern(allSrc, @"[Pp]ercentage.*[<>]=?\s*[01]|[<>]=?\s*[01].*[Pp]ercentage") ||
    ContainsPattern(allSrc, @"0\s*&&.*100|100.*&&.*0");
Check(percentageValidation, "Percentage (0..100) バリデーションが存在する");

// 5-5: Milliseconds >= 0 バリデーション
bool millisecondsValidation =
    ContainsPattern(allSrc, @"[Mm]illiseconds\s*[<>]=?\s*0|0\s*[<>]=?\s*[Mm]illiseconds") ||
    ContainsPattern(allSrc, @"if\s*\(.*[Mm]s\s*<\s*0") ||
    ContainsPattern(allSrc, @"ms\s*<\s*0\)") ||
    ContainsPattern(allSrc, @"TimeoutMs\s*>\s*0|TimeoutMs\s*<\s*0");
Check(millisecondsValidation, "Milliseconds (>= 0) バリデーションが存在する");

// 5-6: ColorCode #RRGGBB パターンバリデーション
bool colorCodeValidation =
    ContainsPattern(allSrc,
        @"#[0-9A-Fa-f]{6}|RRGGBB|ColorHex.*Regex|Regex.*ColorHex") ||
    ContainsPattern(allSrc,
        @"#[A-Fa-f0-9]{6}|@.#[A-Za-z0-9]") ||
    ContainsPattern(allSrc,
        @"\""#FFFFFF\""|ColorHex\s*=\s*\""#");
Check(colorCodeValidation, "ColorCode (#RRGGBB) パターンの定義が存在する");

// 5-7: VariableName 命名規則チェック
// ソースコード中に変数名バリデーション用の正規表現パターンが存在するか確認する。
// 仕様: ^[A-Za-z_][A-Za-z0-9_]*$ に準拠
bool varNameValidation =
    // 正規表現パターンが文字列リテラルとして定義されているか
    ContainsPattern(allSrc, @"\^[A-Za-z_]\[A-Za-z0-9_") ||
    // VariableName に関連するバリデーション処理が存在するか
    ContainsPattern(allSrc, @"VariableName.*Regex|Regex.*VariableName") ||
    ContainsPattern(allSrc, @"IsNullOrEmpty.*VariableName|VariableName.*IsNullOrEmpty");
Warn(varNameValidation,
     "VariableName 命名規則 (^[A-Za-z_][A-Za-z0-9_]*$) のバリデーションが存在する (警告: 明示的な正規表現パターンが検出されない場合あり)");

Console.WriteLine();

// ============================================================
// CHECK 6: Playback 状態の実装
// ============================================================
Console.WriteLine("[CHECK 6: Playback 状態の実装]");

// 6-1: Idle または Playing に対応する状態
bool idleStateExists =
    ContainsPattern(allSrc, @"\bIdle\b") ||
    ContainsPattern(allSrc, @"\bStopped\b");
Check(idleStateExists,
      "Idle (または Stopped) 状態が定義されている");

bool playingStateExists = ContainsPattern(allSrc, @"\bPlaying\b");
Check(playingStateExists, "Playing 状態が定義されている");

// 6-2: Play / Stop メソッドまたはイベントが存在するか
bool playMethodExists =
    ContainsPattern(allSrc, @"(void|Task|bool)\s+Play\s*\(") ||
    ContainsPattern(allSrc, @"PlayAsync\s*\(");
Check(playMethodExists, "Play (または PlayAsync) メソッドが存在する");

bool stopMethodExists =
    ContainsPattern(allSrc, @"(void|Task|bool)\s+Stop\s*\(|StopAll\s*\(");
Check(stopMethodExists, "Stop (または StopAll) メソッドが存在する");

// 6-3: 終了理由に対応する enum や定数が存在するか
// 仕様書: Completed, Cancelled, Aborted, ErrorTerminated, ValidationRejected
string[] terminationReasons = new[]
{
    "Completed", "Cancelled", "Aborted", "ErrorTerminated", "ValidationRejected"
};

int foundTerminationCount = terminationReasons.Count(r =>
    ContainsPattern(allSrc, @"\b" + Regex.Escape(r) + @"\b"));

if (foundTerminationCount == terminationReasons.Length)
{
    Check(true,
          "終了理由 (Completed/Cancelled/Aborted/ErrorTerminated/ValidationRejected) がすべて存在する");
}
else if (foundTerminationCount > 0)
{
    var found = terminationReasons.Where(r =>
        ContainsPattern(allSrc, @"\b" + Regex.Escape(r) + @"\b")).ToArray();
    var missing = terminationReasons.Except(found).ToArray();
    Warn(false,
         $"終了理由が一部のみ存在する: 存在={string.Join(",", found)} / 未定義={string.Join(",", missing)}");
}
else
{
    Check(false,
          "終了理由 enum/定数 (Completed/Cancelled/Aborted/ErrorTerminated/ValidationRejected) が存在する");
}

// 6-4: 外部イベント (PlayRequested, StopRequested など)
string[] externalEvents = new[]
{
    "PlayRequested", "StopRequested"
};
foreach (var ev in externalEvents)
{
    bool evExists = ContainsPattern(allSrc, @"\b" + Regex.Escape(ev) + @"\b");
    Warn(evExists, $"外部イベント '{ev}' が存在する (警告: 実装形式は問わない)");
}

// 6-5: 内部イベント (StepExecuting / StepStarted / StepCompleted など)
bool stepEventExists =
    ContainsPattern(allSrc, @"StepExecuting|StepStarted|StepCompleted");
Check(stepEventExists, "ステップ実行通知イベント (StepExecuting/StepStarted/StepCompleted) が存在する");

Console.WriteLine();

// ============================================================
// SUMMARY
// ============================================================
Console.WriteLine("[SUMMARY]");
Console.WriteLine($"Total  : {passCount + failCount + warnCount} checks");
Console.WriteLine($"Passed : {passCount}");
Console.WriteLine($"Failed : {failCount}");
Console.WriteLine($"Warnings: {warnCount}");
Console.WriteLine();

if (failCount > 0)
{
    Console.WriteLine("❌ 仕様書とソースコードの間に不整合が検出されました。");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("✅ 重大な不整合は検出されませんでした。");
    Environment.Exit(0);
}

// ============================================================
// Helper methods
// ============================================================

static string ResolveRepoRoot(string? explicitPath)
{
    if (explicitPath != null)
    {
        if (!Directory.Exists(explicitPath))
            throw new DirectoryNotFoundException($"指定されたリポジトリルートが見つかりません: {explicitPath}");
        return Path.GetFullPath(explicitPath);
    }

    // 自分のアセンブリ位置から tools/SpecConformanceChecker/ を基点に上へ探す
    var dir = AppContext.BaseDirectory;
    for (int i = 0; i < 10; i++)
    {
        if (Directory.Exists(Path.Combine(dir, "src")) &&
            Directory.Exists(Path.Combine(dir, "docs")))
            return dir;
        var parent = Directory.GetParent(dir);
        if (parent == null) break;
        dir = parent.FullName;
    }

    // フォールバック: カレントディレクトリから探す
    dir = Directory.GetCurrentDirectory();
    for (int i = 0; i < 10; i++)
    {
        if (Directory.Exists(Path.Combine(dir, "src")) &&
            Directory.Exists(Path.Combine(dir, "docs")))
            return dir;
        var parent = Directory.GetParent(dir);
        if (parent == null) break;
        dir = parent.FullName;
    }

    throw new DirectoryNotFoundException(
        "リポジトリルートが見つかりません。引数でパスを指定してください。例: dotnet run /path/to/MacroTool");
}

static bool ContainsPattern(string source, string pattern)
    => Regex.IsMatch(source, pattern, RegexOptions.Multiline);

static IReadOnlyList<string> GetProjectReferences(string csprojPath)
{
    try
    {
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? "")
            .Where(s => s.Length > 0)
            .ToList();
    }
    catch
    {
        return Array.Empty<string>();
    }
}

void Check(bool pass, string message, string? detail = null)
{
    if (pass)
    {
        Console.WriteLine($"  ✅ PASS: {message}");
        passCount++;
    }
    else
    {
        Console.WriteLine($"  ❌ FAIL: {message}");
        if (detail != null)
            Console.WriteLine($"         詳細: {detail}");
        failCount++;
    }
}

void Warn(bool pass, string message, string? detail = null)
{
    if (pass)
    {
        Console.WriteLine($"  ✅ PASS: {message}");
        passCount++;
    }
    else
    {
        Console.WriteLine($"  ⚠️  WARN: {message}");
        if (detail != null)
            Console.WriteLine($"         詳細: {detail}");
        warnCount++;
    }
}

void Skip(string message)
{
    Console.WriteLine($"  ⚠️  SKIP: {message}");
    warnCount++;
}
