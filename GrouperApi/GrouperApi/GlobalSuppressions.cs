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
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Swashbuckle through OperationFilter<T>", Scope = "type", Target = "~T:GrouperApi.AddRequestBodyOperationFilter")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "A missing or empty body is answered with 400 by model binding before the action runs, so a null check here would be unreachable", Scope = "member", Target = "~M:GrouperApi.Controllers.DocumentController.StoreDocumentAsync(GrouperLib.Core.GrouperDocument)~System.Threading.Tasks.Task{Microsoft.AspNetCore.Mvc.IActionResult}")]
