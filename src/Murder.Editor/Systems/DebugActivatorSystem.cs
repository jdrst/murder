﻿using Microsoft.Xna.Framework.Input;
using Bang.Contexts;
using Bang.Systems;
using System.Collections.Immutable;
using Murder.Editor.Components;
using Murder.Editor.Attributes;
using Murder.Core.Geometry;
using Murder.Diagnostics;
using Murder;
using Murder.Editor.Systems;
using Murder.Editor.Utilities;

namespace Road.Editor.Systems
{
    [DoNotPause]
    public class DebugActivatorSystem : IUpdateSystem, IStartupSystem
    {
        private bool _showConsole = false;
        private bool _showEditorSystems = false;

        private ImmutableArray<Type> _debugSystems = ImmutableArray<Type>.Empty;

        public ValueTask Start(Context context)
        {
            if (context.World.TryGetUnique<EditorComponent>() is EditorComponent editorComponent)
            {
                _showEditorSystems = editorComponent.EditorHook.ShowDebug;
            }

            _debugSystems = ReflectionHelper.GetAllTypesWithAttributeDefined<OnlyShowOnDebugViewAttribute>()
                .ToImmutableArray();

            UpdateConsoleSystem(context);
            UpdateEditorSystems(context);

            return default;
        }

        public ValueTask Update(Context context)
        {
            var editorHook = context.World.GetUnique<EditorComponent>().EditorHook;

            if (Game.Input.Shortcut(Keys.F1))
            {
                _showConsole = !_showConsole;
                UpdateConsoleSystem(context);
            }

            if (Game.Input.Shortcut(Keys.F2))
            {
                _showEditorSystems = !_showEditorSystems;
                editorHook.ShowDebug = _showEditorSystems;
                editorHook.StageSize = new Vector2(
                    Game.GraphicsDevice.Viewport.Width,
                    Game.GraphicsDevice.Viewport.Height);

                UpdateEditorSystems(context);
            }

            return default;
        }

        private void UpdateConsoleSystem(Context context)
        {
            if (_showConsole)
            {
                context.World.ActivateSystem<ConsoleSystem>();
            }
            else
            {
                context.World.DeactivateSystem<ConsoleSystem>();
            }

            GameLogger.Toggle(_showConsole);
        }

        private void UpdateEditorSystems(Context context)
        {
            if (_showEditorSystems)
            {
                foreach (Type s in _debugSystems)
                {
                    context.World.ActivateSystem(s);
                }
            }
            else
            {
                foreach (Type s in _debugSystems)
                {
                    context.World.DeactivateSystem(s);
                }
            }
        }
    }
}
