# Semantic Similarity Configuration Templates

This directory contains semantic similarity configuration files that are **automatically loaded** based on file extension.

## 🚀 Convention-Based Auto-Loading

**No configuration needed!** The tool automatically loads the right config for each file type:

- `.sql` files → `sql.semantic-config.json`
- `.py` files → `py.semantic-config.json`
- `.cs` files → `cs.semantic-config.json`
- `.js`/`.ts` files → `js.semantic-config.json`

### Example

```json
{
  "sourcePath": "../sql-repo-a",
  "targetPath": "../sql-repo-b",
  "includeExtensions": [".sql"]
}
```

That's it! The tool automatically:
1. Sees you're comparing `.sql` files
2. Looks for `ConfigTemplates/sql.semantic-config.json`
3. Loads it and applies SQL-specific pattern detection

**Output:**
```
Loading configuration from: my-config.json
Loaded semantic configs for: .sql
Configuration loaded successfully.
```

## 📁 Available Configs (can be extended to any language)

| Config File | Extension | Language | Auto-Detects |
|-------------|-----------|----------|--------------|
| `sql.semantic-config.json` | `.sql` | SQL | CREATE INDEX, PROCEDURE, FUNCTION, VIEW, TRIGGER, TABLE, CONSTRAINT |
| `py.semantic-config.json` | `.py` | Python | class, def, async def, decorators |
| `cs.semantic-config.json` | `.cs` | C# | class, interface, struct, enum, methods, properties, namespace |
| `js.semantic-config.json` | `.js`, `.ts` | JavaScript/TypeScript | class, function, arrow functions, interface, type, exports |

## 🎯 How It Works

### 1. **Convention-Based** (Recommended)
The tool follows this convention:
- Config file name: `{extension}.semantic-config.json`
- Example: `.sql` → `sql.semantic-config.json`

### 2. **Directory Location**
Configs are loaded from `ConfigTemplates/` directory by default.

Change directory in your comparison config:
```json
{
  "semanticConfigDirectory": "MyConfigs"
}
```

### 3. **Priority Rules**

**Highest Priority**: Inline configuration (overrides everything)
```json
{
  "semanticSimilarity": {
    "threshold": 0.50,
    "identifierPatterns": [...]
  }
}
```

**Normal Priority**: Auto-loaded by extension
```
ConfigTemplates/sql.semantic-config.json
ConfigTemplates/py.semantic-config.json
etc.
```

**Disabled**: No config file exists for the extension
```
// No cs.semantic-config.json found
// → Semantic detection disabled for .cs files
```

## ⚙️ Configuration File Structure

Each semantic config contains:

### `threshold` (number, 0.0-1.0)
Minimum similarity score for lines to be considered a modification.
- **Default**: 0.40
- **Lower**: More aggressive (more modifications, fewer add/remove pairs)
- **Higher**: More conservative (more add/remove pairs, fewer modifications)

### `lcsWeight` (number, 0.0-1.0)
Weight given to character-level similarity (Longest Common Subsequence).
- **Default**: 0.70 (70%)
- **Higher**: Prioritize character-level similarity

### `tokenWeight` (number, 0.0-1.0)
Weight given to structural similarity (token-based Jaccard coefficient).
- **Default**: 0.30 (30%)
- **Higher**: Prioritize keyword/identifier similarity

### `identifierPatterns` (array)
Regex patterns to extract semantic identifiers from lines.

Each pattern has:
- **`name`**: Description of the pattern
- **`pattern`**: Regex with capture group 1 for the identifier
- **`options`**: Regex options ("IgnoreCase", "Multiline", "None")
- **`priority`**: Evaluation order (lower = higher priority)

### `commonIdentifiers` (array of strings)
Names to ignore when comparing identifiers. These are **too generic** to be distinctive.

**Purpose**: When two objects have similar names but differ only in a common word, we still want to detect them as different objects.

**Example**:
```sql
CREATE PROCEDURE dbo.GetAccountBalance
CREATE PROCEDURE dbo.GetAccountHistory
```
If "Account" is common, the tool focuses on "Balance" vs "History" to determine they're different procedures.

**What to include**:
- ✅ Language keywords: `SELECT`, `INSERT`, `if`, `else`, `for`
- ✅ Framework/library names: `dbo`, `sys`, `System`, `Console`
- ✅ Common technical terms: `PRIMARY`, `FOREIGN`, `async`, `await`

**What NOT to include**:
- ❌ Domain-specific entities: `Account`, `Customer`, `Order`, `Payment`
- ❌ Business concepts: `Invoice`, `Product`, `User`, `Transaction`

These should be distinctive in your codebase!

**Examples by language**:
- **SQL**: `"dbo"`, `"sys"`, `"PRIMARY"`, `"CHECK"`, `"SELECT"`, `"INSERT"`
- **Python**: `"self"`, `"cls"`, `"init"`, `"str"`, `"main"`
- **C#**: `"this"`, `"System"`, `"void"`, `"async"`, `"await"`
- **JavaScript**: `"this"`, `"console"`, `"window"`, `"document"`

### `tokenDelimiters` (array of strings)
Characters used to split lines into tokens for structural comparison.

Examples: `" "`, `"("`, `")"`, `"["`, `"]"`, `","`, `"."`

## Creating Custom Configs

1. Copy a template that's closest to your language
2. Modify `identifierPatterns` to match your language syntax
3. Update `commonIdentifiers` with language-specific common names
4. Adjust `tokenDelimiters` for your language's syntax
5. Save with a descriptive name (e.g., `java-semantic-config.json`)

### Regex Pattern Tips

- Use capture group 1 `([...])` to extract the identifier to compare
- Use non-capturing groups `(?:...)` for optional parts
- Escape special characters: `\s` (whitespace), `\+` (plus sign), `\.` (period)
- Test patterns with real code samples from your repository

### Example Pattern Breakdown

For Python function definition:
```
def\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(
```

- `def` - Literal "def" keyword
- `\s+` - One or more whitespace
- `([a-zA-Z_][a-zA-Z0-9_]*)` - **Capture group 1**: function name
- `\s*` - Optional whitespace
- `\(` - Opening parenthesis (escaped)

## Disabling Semantic Similarity

To disable semantic similarity detection entirely, simply don't include any of these properties:
- `semanticSimilarity`
- `baseSemanticConfigPath`
- `sourceSemanticConfigPath`
- `targetSemanticConfigPath`

All lines will be compared using only character-level and token-level similarity.

## Troubleshooting

### Too Many "Modified" Lines
- **Increase** the `threshold` (e.g., 0.50 or 0.60)
- **Add more** `identifierPatterns` to detect object names
- **Reduce** `commonIdentifiers` list to be more strict

### Too Many Add/Remove Pairs
- **Decrease** the `threshold` (e.g., 0.30 or 0.35)
- **Remove** overly broad identifier patterns
- **Add more** generic names to `commonIdentifiers`

### Pattern Not Matching
- Test your regex with online tools (regex101.com)
- Check regex `options` (some patterns need "IgnoreCase")
- Ensure capture group 1 extracts the identifier
- Review actual code to confirm pattern matches

## Contributing

To add a new language template:
1. Create `{language}-semantic-config.json` in this directory
2. Include comprehensive patterns for the language
3. Document common identifiers specific to that language
4. Add an entry to this README
5. Test with real codebases in that language
