@Imports System.Web.Http
@Imports ROOT_PROJECT_NAMESPACE.Areas.HelpPage.ModelDescriptions
@ModelType ModelDescription

<div id="body">
    <section class="featured">
        <div class="content-wrapper">
            <p>
                @Html.ActionLink("Help Page Home", "Index")
            </p>
        </div>
    </section>
    <h1>@Model.Name</h1>
    <p>@Model.Documentation</p>
    <section class="content-wrapper main-content clear-fix">
        @Html.DisplayFor(Function(m) Model)
    </section>
</div>

@Section Scripts
    <link type="text/css" href="~/Areas/HelpPage/HelpPage.css" rel="stylesheet" />
End Section

