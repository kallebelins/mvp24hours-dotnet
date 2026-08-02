---
name: 🐛 Bug Report
about: Report a bug to help us improve
title: '[BUG] '
labels: bug
assignees: ''
---

## 🐛 Bug Description

<!-- Clearly and concisely describe the bug -->

## 📋 Steps to Reproduce

Steps to reproduce the behavior:

1. Configure '...'
2. Run '...'
3. Call method '...'
4. See the error

## ✅ Expected Behavior

<!-- Clearly describe what you expected to happen -->

## ❌ Actual Behavior

<!-- Clearly describe what is currently happening -->

## 💻 Code to Reproduce

```csharp
// Paste code that reproduces the problem here
var repository = new Repository<Customer>();
var customer = repository.GetById(1); // Returns null when it should not
```

## 📸 Screenshots

<!-- If applicable, add screenshots to help explain the problem -->

## 🌍 Environment

- **Mvp24Hours Version:** [e.g. 9.1.x]
- **.NET Version:** [e.g. 9]
- **OS:** [e.g. Windows 11, Ubuntu 22.04, macOS 13]
- **IDE:** [e.g. Visual Studio 2022, VS Code, Rider]
- **Database:** [e.g. SQL Server 2022, PostgreSQL 15]

## 📝 Logs and Stack Trace

```
Paste relevant logs and stack traces here
```

## 🔍 Additional Context

<!-- Add any other context about the problem here -->

## ✅ Checklist

- [ ] I checked that an issue about this bug does not already exist
- [ ] I tested with the latest Mvp24Hours version
- [ ] I read the [documentation](https://kallebelins.github.io/mvp24hours-dotnet)
- [ ] I can reproduce the bug consistently

## 🔗 Related Issues

<!-- List related issues if any -->
<!-- Example: Related to #123 -->
