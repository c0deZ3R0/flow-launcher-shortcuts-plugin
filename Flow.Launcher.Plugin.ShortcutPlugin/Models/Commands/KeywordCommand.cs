﻿using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.ShortcutPlugin.Extensions;
using Flow.Launcher.Plugin.ShortcutPlugin.Helper;
using Flow.Launcher.Plugin.ShortcutPlugin.Helper.Interfaces;

namespace Flow.Launcher.Plugin.ShortcutPlugin.Models.Commands;

public class KeywordCommand : ICommand
{
    private readonly IPluginManager _pluginManager;

    public KeywordCommand(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public Command Create()
    {
        var getArgument = CreateGetArgument();
        var setArgument = CreateSetArgument();
        var addArgument = CreateAddArgument();
        var removeArgument = CreateRemoveArgument();

        return new CommandBuilder()
               .WithKey("keyword")
               .WithResponseInfo(("keyword", "Manage plugin keyword"))
               .WithResponseFailure(("Failed to manage plugin keyword", "Something went wrong"))
               .WithArguments(getArgument, setArgument, addArgument, removeArgument)
               .Build();
    }

    private ArgumentLiteral CreateGetArgument()
    {
        return new ArgumentLiteralBuilder()
               .WithKey("get")
               .WithResponseInfo(("keyword get", "Shows all plugin keywords"))
               .WithResponseFailure(("Failed to get plugin keyword", "Something went wrong"))
               .WithResponseSuccess(("Get", "Get plugin keyword"))
               .WithHandler(GetKeywordCommandHandler)
               .Build();
    }

    private ArgumentLiteral CreateSetArgument()
    {
        var argument = new ArgumentBuilder()
                       .WithResponseInfo(("Enter new keyword", "How should your plugin be called?"))
                       .WithResponseSuccess(("Set", "Your plugin keyword will be set"))
                       .WithHandler(SetKeywordCommandHandler)
                       .Build();

        return new ArgumentLiteralBuilder()
               .WithKey("set")
               .WithResponseInfo(("keyword set", "Set plugin keyword. Other keywords will be removed"))
               .WithResponseFailure(("Failed to set plugin keyword", "Something went wrong"))
               .WithArgument(argument)
               .Build();
    }

    private ArgumentLiteral CreateAddArgument()
    {
        var argument = new ArgumentBuilder()
                       .WithResponseInfo(("Enter new keyword", "How should your plugin be called?"))
                       .WithResponseSuccess(("Add", "Your plugin keyword will be added"))
                       .WithHandler(AddKeywordCommandHandler)
                       .Build();

        return new ArgumentLiteralBuilder()
               .WithKey("add")
               .WithResponseInfo(("keyword add", "Add additional plugin keyword"))
               .WithResponseFailure(("Failed to add plugin keyword", "Something went wrong"))
               .WithArgument(argument)
               .Build();
    }

    private ArgumentLiteral CreateRemoveArgument()
    {
        var argument = new ArgumentBuilder()
                       .WithResponseInfo(("Enter keyword", "Which keyword should be removed?"))
                       .WithResponseSuccess(("Remove", "Your plugin keyword will be removed"))
                       .WithHandler(RemoveKeywordCommandHandler)
                       .Build();

        return new ArgumentLiteralBuilder()
               .WithKey("remove")
               .WithResponseInfo(("keyword remove", "Remove plugin keyword"))
               .WithResponseFailure(("Failed to remove plugin keyword", "Something went wrong"))
               .WithArgument(argument)
               .Build();
    }

    private List<Result> GetKeywordCommandHandler(ActionContext context, ParsedQuery parsedQuery)
    {
        return ResultExtensions.SingleResult("Plugin keywords",
            string.Join(", ", _pluginManager.Context.CurrentPluginMetadata.ActionKeywords));
    }

    private List<Result> SetKeywordCommandHandler(ActionContext context, ParsedQuery parsedQuery)
    {
        var arguments = parsedQuery.CommandArguments;
        var newKeyword = arguments[2];

        return ResultExtensions.SingleResult("Setting plugin keyword (other will be removed)",
            $"New keyword will be `{newKeyword}`", () =>
            {
                var metadata = _pluginManager.Context.CurrentPluginMetadata;

                metadata.ActionKeywords
                        .ToList()
                        .ForEach(oldKeyword => { _pluginManager.API.RemoveActionKeyword(metadata.ID, oldKeyword); });

                _pluginManager.API.AddActionKeyword(metadata.ID, newKeyword);
                _pluginManager.API.SaveAppAllSettings();
            });
    }

    private List<Result> AddKeywordCommandHandler(ActionContext context, ParsedQuery parsedQuery)
    {
        var arguments = parsedQuery.CommandArguments;
        var metadata = _pluginManager.Context.CurrentPluginMetadata;
        var actionKeywords = metadata.ActionKeywords;
        var newKeyword = arguments[2];

        if (actionKeywords.Contains(newKeyword))
        {
            return ResultExtensions.SingleResult("Add plugin keyword", $"Keyword `{newKeyword}` already exists");
        }

        return ResultExtensions.SingleResult("Add plugin keyword", $"New keyword added will be `{newKeyword}`",
            () =>
            {
                _pluginManager.API.AddActionKeyword(metadata.ID, newKeyword);
                _pluginManager.API.SaveAppAllSettings();
            });
    }

    private List<Result> RemoveKeywordCommandHandler(ActionContext context, ParsedQuery parsedQuery)
    {
        var arguments = parsedQuery.CommandArguments;
        var metadata = _pluginManager.Context.CurrentPluginMetadata;
        var actionKeywords = metadata.ActionKeywords;

        if (actionKeywords.Count == 1)
        {
            return ResultExtensions.SingleResult("Remove plugin keyword", "You can't remove the only keyword");
        }

        if (!actionKeywords.Contains(arguments[2]))
        {
            return ResultExtensions.SingleResult("Remove plugin keyword", $"Keyword `{arguments[2]}` doesn't exist");
        }

        return ResultExtensions.SingleResult("Remove plugin keyword", $"Keyword `{arguments[2]}` will be removed",
            () =>
            {
                _pluginManager.API.RemoveActionKeyword(metadata.ID, arguments[2]);
                _pluginManager.API.SaveAppAllSettings();
            });
    }
}