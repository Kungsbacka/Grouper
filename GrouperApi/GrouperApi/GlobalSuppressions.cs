// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Not applicable in ASP.NET controllers", Scope = "namespaceanddescendants", Target = "~N:GrouperApi")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.DocumentController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.AuditLogController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.TestController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.ErrorController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.OperationalLogController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.GroupInfoController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.EventLogController")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "API Controllers should be public", Scope = "type", Target = "~T:GrouperApi.Controllers.GrouperController")]
[assembly: SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Created non static by the ASP.NET API template", Scope = "type", Target = "~T:GrouperApi.Program")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Created public by the ASP.NET API template", Scope = "type", Target = "~T:GrouperApi.Program")]
