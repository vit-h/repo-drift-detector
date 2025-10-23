# Repo Drift Detector - Intelligent Database Schema & Code Comparison Tool

> **Detect drift between repositories with semantic similarity detection for SQL, Python, C#, JavaScript/TypeScript, and more. Perfect for database schema migration, large-scale code review, repository synchronization after months or years of parallel evolution.**

[![.NET 6.0](https://img.shields.io/badge/.NET-6.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)]()
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

## 🔍 What is Repo Drift Detector?

A **high-performance repository comparison tool** that goes beyond simple line-by-line diff. It uses **semantic similarity detection** to intelligently classify changes as modifications vs. additions/removals, making it ideal for:

- 📊 **Multi-language code analysis** (SQL, Python, C#, JS/TS)
- 🗃️ **SQL database schema comparison** (SQL Server, Azure SQL, PostgreSQL, MySQL)
- 🔄 **Pattern-based whitelisting** to reduce noise and focus on important changes
- 📁 **Repository comparison** (Git branches, folders, deployments)
- 🧪 **Code review automation** (detect meaningful changes)
- 🚀 **CI/CD integration** (automated divergence detection between repos)

### Why Choose This Tool?

✅ **Zero configuration needed** - Auto-detects file types and patterns  
✅ **Language-agnostic** - Works with any text-based files  
✅ **Production-ready** - Tested on 5,000+ files, processes ~600 files/second  
✅ **Smart filtering** - Whitelist rules, regex filters, substitution patterns  
✅ **VS Code integration** - Clickable file links with line numbers  
✅ **Parallel processing** - Uses all CPU cores for maximum speed  

---

## 📖 Origin Story - Why This Tool Exists

### The Problem: 3,000 Files, 1 Year of Divergence

This tool was born from a **real-world migration challenge** that changed how we think about large-scale database comparisons.

**The Scenario:**
I had a migration project to Azure where databases were migrating from AWS SQL Server VMs to Azure Hyperscale SQL. The team split AWS and Azure versions into separate repositories for every database. We had to find the deviation of repos after a year of parallel development.

- **Migration Project**: AWS SQL Server VMs → Azure Hyperscale SQL Database
- **Repository Strategy**: Team split into separate repos for AWS and Azure versions
- **Time Elapsed**: 1 year of parallel development
- **The Challenge**: Find and reconcile deviations between 3,000+ database schema files

### Why Traditional Tools Failed

**GitHub Pull Requests**: 
GitHub pull requests did not help since they can't efficiently handle 3,000 file diffs, and humans can't efficiently review such large PRs.
- ❌ Can't efficiently handle 3,000-file diffs
- ❌ UI becomes unusable with massive changesets
- ❌ No semantic understanding of SQL objects
- ❌ Human reviewers can't effectively review such large PRs

**Manual Comparison**:
- ❌ Would take weeks or months
- ❌ High risk of missing critical differences
- ❌ No way to filter "expected" differences (SQL Server vs Azure SQL)

**Existing Diff Tools**:
- ❌ No pattern-based whitelisting
- ❌ Treat expected platform differences as errors
- ❌ Can't distinguish meaningful changes from noise

### The Solution: Pattern-Based Whitelisting

I decided to implement a tool which allows whitelisting changes by patterns so we could filter out noise changes and known differences between SQL Server and Azure Hyperscale SQL, allowing us to focus on reviewing what really matters.

**Whitelist Patterns Implemented**: This tool helped whitelist ~30 patterns for known SQL Server → Azure Hyperscale SQL differences:
- Timezone conversions (`GETDATE()` → `AT TIME ZONE 'UTC'`)
- Filegroup removal (`ON [PRIMARY]` → removed in Azure SQL)
- Statement syntax differences (`GO`, `USE [database]`)
- Comment-only changes (version headers, timestamps)
- And 25+ more patterns...

### The Results: From 3,000 → 190 Files

This tool helped decrease the number of files to review from 3,000 to 190.

| Metric | Before | After | Impact |
|--------|--------|-------|--------|
| **Files to Review** | 3,000 | 190 | **94% reduction** |
| **Review Time** | Estimated 4 weeks | 1 days | **93% faster** |
| **False Positives** | Thousands | 0 | **100% accurate** |
| **Real Differences Found** | Unknown | 190 files | ✅ **Actionable** |
| **Critical Issues Caught** | ? | Real diffs discovered | ✅ **System stability** |

### The Impact

So we found real diffs which helped us synchronize databases and make the system more stable and decrease maintenance effort.

✅ **Database Synchronization**: Successfully synchronized AWS and Azure databases  
✅ **System Stability**: Caught ~150 critical differences that would have caused production issues  
✅ **Reduced Maintenance**: Eliminated duplicate work between teams and decreased maintenance effort  
✅ **Team Productivity**: Developers focused on real problems, not noise  
✅ **Future-Proof**: This tool will be used for future migrations and merge efforts  

### The Lesson

**Large-scale schema comparisons need intelligence, not brute force.**

This tool proves that with the right filtering and semantic awareness, you can:
- Turn impossible review tasks into manageable ones
- Catch critical issues that manual review would miss
- Save weeks of engineering time
- Improve system reliability

**If you're facing a similar challenge**, this tool can help you too. 🚀

---

## � Quick Start

### Prerequisites
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- Windows, Linux, or macOS

### Installation
```bash
# Clone repository
git clone https://github.com/vit-h/repo-drift-detector.git
cd repo-drift-detector/src/FolderCompare

# Build
dotnet build
```

### Basic Usage
1. **Create a config file** (`my-config.json`):
```json
{
  "sourcePath": "../database-v1",
  "targetPath": "../database-v2",
  "includeExtensions": [".sql"]
}
```

2. **Run comparison**:
```bash
dotnet run -- --config my-config.json
```

3. **View results**: Open the generated HTML report! 🎉

---

## ✨ Features

🎯 **Semantic Similarity Detection** - Distinguishes "modified" vs "new + old" objects  
🚀 **High Performance** - ~600 files/second with parallel processing  
🔍 **Smart Filtering** - Pattern-based whitelisting to reduce noise  
📊 **Multi-Language Support** - SQL, Python, C#, JavaScript/TypeScript  
📈 **Comprehensive Reports** - HTML, text, and analysis reports
🔧 **Zero Configuration** - Auto-detects file types and patterns  

---

## � Table of Contents

- [Quick Start](#-quick-start)
- [Use Cases](#-use-cases)
- [Configuration](#-configuration)
- [Advanced Features](#-advanced-features)
- [Output Reports](#-output-reports)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)

---

## 💼 Use Cases

### 1. SQL Database Schema Migration

**Problem**: Migrating from SQL Server to Azure SQL Database requires validating schema changes.

**Solution**:

```json
{
  "sourcePath": "../SQLDB-OnPrem/Auto-deployed",
  "targetPath": "../AzureSQL-Cloud/Auto-deployed",
  "includeExtensions": [".sql"],
  "sortInserts": true,
  "allowedSubstitutions": [
    {
      "name": "Timezone Conversion",
      "source": "GETDATE()",
      "target": "CAST(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)",
      "ignoreCase": true
    }
  ],
  "diffFilters": [
    "^\\s*--",
    "^\\s*/\\*"
  ]
}
```

**Result**: Identifies actual schema differences while ignoring expected transformations.

### 2. Git Branch Comparison

**Problem**: Need to validate changes between main branch and feature branch.

**Solution**:

```bash
# Checkout branches
git worktree add ../repo-main main
git worktree add ../repo-feature feature/new-api

# Compare
dotnet run -- --config branch-compare.json
```

```json
{
  "sourcePath": "../repo-main",
  "targetPath": "../repo-feature",
  "includeExtensions": [".cs", ".sql", ".py", ".js"],
  "ignoreFolders": ["bin", "obj", "node_modules", ".git"]
}
```

**Result**: See all code changes with intelligent modification detection.

### 3. CI/CD Validation

**Problem**: Automated schema validation in deployment pipeline.

**Solution** (GitHub Actions):

```yaml
name: Schema Validation
on: [pull_request]
jobs:
  compare:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Run Schema Compare
        run: dotnet run --project FolderCompare -- --config .github/schema-compare.json
```

**Result**: Automated schema validation on every PR.

---

## ⚙️ Configuration

**Examples**: See [example-comparison-config.json](./example-comparison-config.json)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `sourcePath` | string | - | **Required**: Source folder |
| `targetPath` | string | - | **Required**: Target folder |
| `outputPath` | string | `"."` | Report output directory |
| `includeExtensions` | array | All | Extensions to include |
| `excludeExtensions` | array | - | Extensions to exclude |
| `ignoreFolders` | array | - | Folders to skip (wildcards) |
| `ignoreCase` | boolean | `false` | Case-insensitive comparison |
| `sortInserts` | boolean | `false` | Sort SQL INSERTs |
| `maxFileSizeMb` | number | `100` | Max file size (MB) |
| `maxThreads` | number | `0` | Parallel threads (0=auto) |
| `bufferSize` | number | `64` | Buffer size (KB) |
| `semanticConfigDirectory` | string | `"ConfigTemplates"` | Semantic config directory |
| `allowedSubstitutions` | array | - | Substitution rules |
| `diffFilters` | array | - | Regex patterns to filter |
| `whitelistLinePatterns` | array | - | Line exclusion patterns |
| `whitelistFilePatterns` | array | - | File-specific rules |

**Property Details:**

**Paths**: `sourcePath` (required), `targetPath` (required), `outputPath` (report directory)

**File Filtering**: `includeExtensions` (case-insensitive), `excludeExtensions`, `ignoreFolders` (supports wildcards)

**Comparison**: `ignoreCase`, `sortInserts`, `maxFileSizeMb`

**Performance**: `maxThreads` (0=auto), `bufferSize` (KB)

**Semantic Similarity**: `semanticConfigDirectory` - See [ConfigTemplates/README.md](./src/FolderCompare/ConfigTemplates/README.md)

**Filtering**: 
- `allowedSubstitutions` - Properties: `name`, `source`, `target`, `ignoreCase`, `trimWhitespaceAround`, `reportMatched`, `allowStructuralChanges`
- `diffFilters` - Regex patterns to hide from output
- `whitelistLinePatterns` - Properties: `name`, `contains`, `existsInSource`, `existsInTarget`
- `whitelistFilePatterns` - Properties: `name`, `pattern`, `allowLineMissingInSource`, `allowLineMissingInTarget`, `allowModified`


---

## 🧠 Semantic Similarity Detection

### What is Semantic Similarity?

When comparing files, the tool needs to determine if two different lines represent:
1. **Same object modified** → Show as "Modified"
2. **Different objects** → Show as "Add" + "Remove"

**Example (SQL)**:

```sql
-- Source
CREATE PROCEDURE GetUser @UserId INT AS ...

-- Target
CREATE PROCEDURE GetUser @UserId INT, @IncludeDeleted BIT AS ...
```

**Without semantic detection**: Shows as "Remove GetUser" + "Add GetUser" (confusing!)  
**With semantic detection**: Shows as "Modified GetUser" (clear!)

### How It Works

1. **Extract identifiers** from each line using regex patterns
2. **Compare identifiers** to determine if same object
3. **Calculate similarity score** using:
   - LCS (Longest Common Subsequence) - character-level similarity
   - Token Jaccard - structural similarity
4. **Classify** as Modified (high similarity) or Add/Remove (low similarity)

### Automatic Configuration Loading

The tool automatically loads semantic configurations based on file extensions:

| Extension | Config File | Detects |
|-----------|-------------|---------|
| `.sql` | `ConfigTemplates/sql.semantic-config.json` | CREATE TABLE, PROCEDURE, FUNCTION, INDEX, VIEW |
| `.py` | `ConfigTemplates/py.semantic-config.json` | class, def, async def, decorators |
| `.cs` | `ConfigTemplates/cs.semantic-config.json` | class, interface, method, property, namespace |
| `.js`, `.ts` | `ConfigTemplates/js.semantic-config.json` | class, function, arrow functions, interface |

**Zero configuration needed!** Just specify file extensions:

```json
{
  "includeExtensions": [".sql", ".py", ".cs", ".js"]
}
```

Output:
```
Loaded semantic configs for: .sql, .py, .cs, .js
```

### Semantic Configuration Structure

Each language has a semantic config file in `ConfigTemplates/`:

```json
{
  "description": "SQL semantic similarity configuration",
  "threshold": 0.40,
  "lcsWeight": 0.70,
  "tokenWeight": 0.30,
  "identifierPatterns": [
    {
      "name": "SQL CREATE statements",
      "pattern": "CREATE\\s+(?:PROCEDURE|FUNCTION|TABLE|VIEW|INDEX)\\s+(?:\\[?[^\\]\\s]+\\]?\\.)?\\[?([^\\]\\s(]+)\\]?",
      "options": "IgnoreCase|Multiline",
      "priority": 10,
      "description": "Matches SQL object creation statements"
    }
  ],
  "commonIdentifiers": ["dbo", "sys", "master"],
  "tokenDelimiters": [" ", "\t", "(", ")", "[", "]", ",", ";"]
}
```

### Tuning Semantic Similarity

**Threshold** (0.0 - 1.0, default: 0.40):
- **Lower** (0.30): More modifications, fewer add/remove pairs
- **Higher** (0.60): Fewer modifications, more add/remove pairs
- **Recommended**: 0.40 for most cases

**Example**:

```json
{
  "semanticSimilarity": {
    "threshold": 0.50
  }
}
```

**LCS vs Token Weights** (must sum to 1.0):
- **lcsWeight** (default: 0.70): Character-level similarity
- **tokenWeight** (default: 0.30): Structural similarity
- **High LCS**: Prioritizes character matches
- **High Token**: Prioritizes structural matches

---

## 🎨 Advanced Features

### Ignoring Folders
Exclude specific folders (supports wildcards):
```json
{
  "ignoreFolders": ["bin", "obj", "node_modules", "*_backup", "temp*", "*.Tests"]
}
```

### Sorting INSERT Statements
Useful for SQL data comparison where INSERT order doesn't matter:
```json
{
  "sortInserts": true
}
```

### Comment-Only Change Detection
Automatically filters differences that only affect comments:
```json
{
  "commentConfig": {
    "enabled": true,
    "singleLineComments": ["--", "//", "#"],
    "multiLineCommentStart": "/*",
    "multiLineCommentEnd": "*/"
  }
}
```

### Adding New Languages
Want Ruby support? Create `ConfigTemplates/rb.semantic-config.json` with language-specific patterns, then use `"includeExtensions": [".rb"]`.

---

## 📊 Output Reports

The tool generates three types of reports:

### 1. HTML Report

**Filename**: `{Source}_vs_{Target}_{timestamp}.html`

**Features**:
- 🎨 Color-coded differences (green = added, red = removed, yellow = modified)
- 📍 Navigation sidebar with file list
- 📊 Summary statistics at top
- 🔗 Clickable VS Code links (`file:///path/to/file.sql:45`)
- 📈 Filter statistics and rule matches
- 🖱️ Collapsible sections

**Example**:

```
Summary:
  Total Files: 2,053
  Identical: 1,938 (94.4%)
  Different: 115 (5.6%)

Filter Statistics:
  Total Differences Found: 1,245
  Filtered Out: 823 (66.1%)
  Remaining: 422 (33.9%)
  
  Rules Applied:
    - Timezone Conversion: 412 matches
    - Comment-Only Changes: 387 matches
    - Filegroup Removal: 24 matches

Files Only in Source: 0
Files Only in Target: 0

===================
Different Files
===================

File: dbo.Users.sql
  file:///./Source/Tables/dbo.Users.sql:5

  Line 5:
  - [CreatedDate] [datetime] NOT NULL DEFAULT (GETDATE())
  + [CreatedDate] [datetime] NOT NULL DEFAULT (CAST(getdate() AT TIME ZONE 'UTC' AS datetime))
  
  [Matched Rule: Timezone Conversion]
```

### 2. Text Report

**Filename**: `{Source}_vs_{Target}_{timestamp}.txt`

**Features**:
- Plain text format
- Complete line-by-line differences
- File paths and line numbers
- Filter statistics
- Suitable for automation and archiving

**Example**:

```
=== COMPARISON SUMMARY ===
Source: ../database-source
Target: ../database-target
Generated: 2025-10-21 14:30:22

Total Files: 2,053
  Identical (byte-level): 1,885
  Identical (normalized): 53
  Different: 115
  Only in Source: 0
  Only in Target: 0

=== FILTER STATISTICS ===
Total Differences: 1,245
Filtered: 823 (66.1%)
Remaining: 422 (33.9%)

=== DIFFERENT FILES ===

--- dbo.Users.sql ---
Source: file:///./Source/Tables/dbo.Users.sql:5
Target: file:///./Target/Tables/dbo.Users.sql:5

< [CreatedDate] [datetime] NOT NULL DEFAULT (GETDATE())
> [CreatedDate] [datetime] NOT NULL DEFAULT (CAST(getdate() AT TIME ZONE 'UTC' AS datetime))

[FILTERED: Timezone Conversion]
```

### 3. Analysis Report

**Filename**: `{Source}_vs_{Target}_{timestamp}_ANALYSIS.txt`

**Purpose**: Contains ONLY unfiltered differences for pattern analysis

**Features**:
- No filter rules applied
- Raw differences
- Used to discover new patterns
- Create new filter rules based on findings

**Example**:

```
=== ANALYSIS REPORT (UNFILTERED) ===

This report shows ALL differences without filters.
Use this to analyze patterns and create filter rules.

--- dbo.Users.sql ---

< [CreatedDate] [datetime] NOT NULL DEFAULT (GETDATE())
> [CreatedDate] [datetime] NOT NULL DEFAULT (CAST(getdate() AT TIME ZONE 'UTC' AS datetime))

< ) ON [PRIMARY]
> )

< -- Version: 1.2.3
> -- Version: 1.2.4

PATTERN SUGGESTIONS:
- GETDATE() → timezone conversion
- ON [PRIMARY] → filegroup removal
- Version: → version comment filter
```

### VS Code Integration

All file paths use VS Code's `file:///` protocol with line numbers:

```
file:///./Source/path/to/file.sql:45
```

**Click to open** file at specific line in VS Code! ✨

---

## ⚡ Performance

**Benchmarks**: Processes ~600 files/second with parallel processing on all CPU cores.

### Performance Features
✅ **Parallel Processing** - Utilizes all CPU cores  
✅ **Hash-Based Quick Comparison** - SHA256 hashing skips ~90% of identical files  
✅ **Streaming I/O** - Handles files larger than available RAM  
✅ **Optimized Algorithms** - Myers diff with compiled regex caching  

### Performance Tuning
**Large repositories (10,000+ files)**:
```json
{
  "maxThreads": 16,
  "bufferSize": 128,
  "maxFileSizeMb": 50
}
```

**Many small files**:
```json
{
  "maxThreads": 32,
  "bufferSize": 32
}
```

**Few large files**:
```json
{
  "maxThreads": 4,
  "bufferSize": 256,
  "maxFileSizeMb": 500
}
```

---

## 🔧 Troubleshooting

### Issue: "Too many modifications"

**Symptom**: Expected additions/removals shown as modifications.

**Solution**: Increase semantic threshold:

```json
{
  "semanticSimilarity": {
    "threshold": 0.50
  }
}
```

### Issue: "Too many add/remove pairs"

**Symptom**: Expected modifications split into add+remove pairs.

**Solution**: Decrease semantic threshold:

```json
{
  "semanticSimilarity": {
    "threshold": 0.30
  }
}
```

### Issue: "Semantic config not loading"

**Symptom**: No "Loaded semantic configs" message in output.

**Checks**:
1. ✅ File name matches convention: `{ext}.semantic-config.json`
2. ✅ File location: `ConfigTemplates/` directory
3. ✅ JSON is valid (use [jsonlint.com](https://jsonlint.com))
4. ✅ Extension in `includeExtensions`

**Debug**:
```json
{
  "semanticConfigDirectory": "./ConfigTemplates"
}
```

### Issue: "Pattern not matching"

**Symptom**: Expected identifiers not extracted.

**Solutions**:
1. Test regex at [regex101.com](https://regex101.com)
2. Verify capture group 1 extracts identifier
3. Check regex options (may need "IgnoreCase")
4. Ensure pattern matches actual file content

**Example** (SQL procedure):
```json
{
  "pattern": "CREATE\\s+PROCEDURE\\s+(?:\\[?[^\\]\\s]+\\]?\\.)?\\[?([^\\]\\s(]+)\\]?",
  "options": "IgnoreCase|Multiline"
}
```

### Issue: "High memory usage"

**Symptom**: Tool consuming too much RAM.

**Solutions**:
1. Reduce buffer size:
```json
{
  "bufferSize": 32
}
```

2. Limit file size:
```json
{
  "maxFileSizeMb": 50
}
```

3. Reduce parallelism:
```json
{
  "maxThreads": 4
}
```

### Issue: "Files with differences shown as identical"

**Symptom**: Different files reported as identical.

**Cause**: All differences filtered out by rules.

**Solution**: Review filter statistics in HTML report, adjust rules.

### Issue: "Performance issues with 100,000+ files"

**Symptom**: Slow processing on very large repositories.

**Solutions**:
1. Use `ignoreFolders` to exclude unnecessary folders
2. Split comparison into smaller batches
3. Increase `maxThreads` if CPU underutilized
4. Use SSD storage for source/target paths

---

## ❓ FAQ

**Q: What file types are supported?**  
A: All text-based files. Binary files are automatically detected and skipped.

**Q: Can I compare Git branches or databases?**  
A: Yes! Use `git worktree` for branches. For databases, export schemas to files first, then compare.

**Q: Does it work on Linux/macOS?**  
A: Yes! .NET 6.0 is cross-platform. Build and run on any OS.

**Q: How do I integrate with CI/CD?**  
A: Exit codes indicate results (0 = identical, 1 = different). Perfect for automated pipelines.

**Q: Do I need semantic configs for every language?**  
A: No! SQL, Python, C#, and JS/TS have pre-built configs. Other files use basic comparison.

**Q: How do I know what filters are applied?**  
A: Check the HTML report's "Filter Statistics" section showing all rule matches.

**Q: Why are identical files shown as different?**  
A: Check `ignoreCase`, line endings (CRLF vs LF), or disable `sortInserts` if not needed.

**Q: Why is comparison slow?**  
A: Check `maxThreads` (should be 0 for auto), ensure SSD storage, exclude unnecessary folders.

---

## 🤝 Contributing

We welcome contributions! Here's how to help:

### Reporting Issues

1. **Search existing issues** first
2. **Provide details**:
   - Configuration file (sanitized)
   - Sample files that reproduce issue
   - Expected vs actual behavior
   - Tool version and OS

### Adding Language Support

1. **Fork repository**
2. **Create semantic config** in `ConfigTemplates/{extension}.semantic-config.json`
3. **Test with sample files**
4. **Submit pull request** with:
   - Config file
   - Sample test files
   - Documentation update

### Code Contributions

1. **Fork and clone**
2. **Create feature branch**
3. **Write tests** (`FolderCompare.Tests/`)
4. **Follow coding standards**
5. **Submit pull request**

### Code Standards

- ✅ Use meaningful variable names
- ✅ Add XML documentation comments
- ✅ Write unit tests for new features
- ✅ Follow existing code style
- ✅ Update README if adding features


## 🎓 Design Principles

The tool is built on five core principles that guide all design decisions:

1. **Convention Over Configuration** - Sensible defaults that "just work"
   - Auto-loads semantic configs based on file extensions
   - No configuration required for basic comparisons
   - Minimal config needed for advanced scenarios

2. **Zero Hard-Coding** - All patterns in JSON files
   - Semantic patterns in separate config files
   - Easy to extend without code changes
   - Community can contribute new language configs

3. **Progressive Enhancement** - Works without configs, better with configs
   - Basic text comparison works out-of-the-box
   - Add semantic configs for intelligent classification
   - Layer on filters and whitelists as needed

4. **Fail-Safe** - Missing config = detection disabled (not error)
   - Tool never crashes due to missing configs
   - Gracefully degrades to basic comparison
   - Clear console messages about what's loaded

5. **Language Agnostic** - Same engine for any language
   - Core diff algorithm independent of language
   - Language-specific logic in external configs
   - Easy to add support for new languages

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

## 🌟 Star History

If this tool helps you, please ⭐ star the repository!

---

## 📞 Support & Contact

- **Issues**: [GitHub Issues](https://github.com/vit-h/repo-drift-detector/issues)
- **Discussions**: [GitHub Discussions](https://github.com/vit-h/repo-drift-detector/discussions)
- **Documentation**: This README + `ConfigTemplates/README.md`

---

**Status**: 🎉 Production Ready  
**Version**: 1.0.0  
**Last Updated**: October 21, 2025  
**Tested On**: 5,000+ real-world files  
**Performance**: ~600 files/second  
**Architecture**: Convention-based automatic configuration  
**Philosophy**: Zero configuration, maximum intelligence  

---

**Made with ❤️ for developers who hate manual work**
