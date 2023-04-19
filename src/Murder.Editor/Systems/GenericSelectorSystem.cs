﻿using Bang;
using Bang.Entities;
using ImGuiNET;
using Murder.Assets;
using Murder.Components;
using Murder.Core;
using Murder.Core.Geometry;
using Murder.Core.Graphics;
using Murder.Core.Input;
using Murder.Editor.Components;
using Murder.Editor.EditorCore;
using Murder.Editor.Utilities;
using Murder.Services;
using Murder.Utilities;
using System.Collections.Immutable;

namespace Murder.Editor.Systems
{
    public class GenericSelectorSystem
    {
        internal const float DRAG_MIN_DURATION = 0.25f;

        private readonly Vector2 _selectionBox = new Point(12, 12);
        private Vector2 _offset = Vector2.Zero;

        /// <summary>
        /// Entity that is being dragged, if any.
        /// </summary>
        private Entity? _dragging = null;

        private float _dragTimer = 0;

        private bool _isShowingImgui = false;

        private Point? _startedGroupInWorld;
        private Rectangle? _currentAreaRectangle;

        public void StartImpl(World world)
        {
            if (world.TryGetUnique<EditorComponent>()?.EditorHook is EditorHook hook)
            {
                hook.OnEntitySelected += OnEntityToggled;
            }
        }

        /// <summary>
        /// This is only used for rendering the entity components during the game (on debug mode).
        /// </summary>
        public void DrawGuiImpl(World world, ImmutableArray<Entity> entities)
        {
            _isShowingImgui = true;

            EditorHook hook = world.GetUnique<EditorComponent>().EditorHook;

            // Entity List
            ImGui.SetNextWindowBgAlpha(0.9f);
            ImGui.SetNextWindowSizeConstraints(
                new System.Numerics.Vector2(300, 300),
                new System.Numerics.Vector2(600, 768)
            );

            ImGui.Begin("Hierarchy");

            ImGui.SetWindowPos(new(0, 250), ImGuiCond.Appearing);

            ImGui.BeginChild("hierarchy_entities");
            foreach (var entity in entities)
            {
                var name = $"Instance";
                if (entity.TryGetComponent<PrefabRefComponent>(out var assetComponent))
                {
                    if (Game.Data.TryGetAsset<PrefabAsset>(assetComponent.AssetGuid) is PrefabAsset asset)
                    {
                        name = asset.Name;
                    }
                }

                if (ImGui.Selectable($"{name}({entity.EntityId})##{name}_{entity.EntityId}", hook.IsEntitySelected(entity.EntityId)))
                {
                    hook.SelectEntity(entity, clear: true);
                }
                if (ImGui.IsItemHovered())
                {
                    hook.HoverEntity(entity);
                }
            }

            ImGui.EndChild();
            ImGui.End();

            foreach ((_, Entity e) in hook.AllSelectedEntities)
            {
                ImGui.SetNextWindowBgAlpha(0.9f);
                ImGui.SetNextWindowDockID(42, ImGuiCond.Appearing);

                if (hook.DrawEntityInspector is not null && !hook.DrawEntityInspector(e))
                {
                    hook.UnselectEntity(e);
                }
            }
        }

        public void Update(World world, ImmutableArray<Entity> entities)
        {
            EditorHook hook = world.GetUnique<EditorComponent>().EditorHook;
            if (hook.UsingCursor)
            // Someone else is using our cursor, let's wait out turn.
            {
                _startedGroupInWorld = null;
                _currentAreaRectangle = null;
                _dragging = null;
                return;
            }

            if (hook.EntityToBePlaced is not null)
            {
                // An entity will be placed, skip this.
                return;
            }

            // If user has selected to destroy entities.
            if (Game.Input.Pressed(MurderInputButtons.Delete))
            {
                foreach ((_, Entity e) in hook.AllSelectedEntities)
                {
                    hook.RemoveEntityWithStage?.Invoke(e.EntityId);
                }

                hook.UnselectAll();
            }

            if (_dragging?.IsDestroyed ?? false)
            {
                _dragging = null;
            }

            bool clicked = Game.Input.Pressed(MurderInputButtons.LeftClick);
            bool released = Game.Input.Released(MurderInputButtons.LeftClick);

            MonoWorld monoWorld = (MonoWorld)world;
            Point cursorPosition = monoWorld.Camera.GetCursorWorldPosition(hook.Offset, new(hook.StageSize.X, hook.StageSize.Y));

            hook.CursorWorldPosition = cursorPosition;
            hook.CursorScreenPosition = Game.Input.CursorPosition - hook.Offset;

            Rectangle bounds = new(hook.Offset, hook.StageSize);
            bool hasFocus = bounds.Contains(Game.Input.CursorPosition);

            ImmutableDictionary<int, Entity> selectedEntities = hook.AllSelectedEntities;
            bool isMultiSelecting = Game.Input.Down(MurderInputButtons.Shift) || selectedEntities.Count > 1;

            bool clickedOnEntity = false;
            foreach (Entity e in entities)
            {
                if (!e.HasTransform()) continue;

                Vector2 position = e.GetGlobalTransform().Vector2;
                Rectangle rect = new(position - _selectionBox / 2f, _selectionBox);

                if (e.Parent is not null && !hook.EnableSelectChildren)
                {
                    // We block dragging entities on world editors otherwise it would be too confusing (signed: Pedro).
                    continue;
                }

                if (hasFocus && rect.Contains(cursorPosition))
                {
                    hook.Cursor = CursorStyle.Point;

                    if (!hook.IsEntityHovered(e.EntityId))
                    {
                        hook.HoverEntity(e);
                    }

                    if (clicked)
                    {
                        hook.SelectEntity(e, clear: !isMultiSelecting);
                        clickedOnEntity = true;

                        _offset = e.GetGlobalTransform().Vector2 - cursorPosition;
                        _dragging = e;
                    }

                    if (_dragging == e && Game.Input.Down(MurderInputButtons.LeftClick))
                    {
                        _dragTimer += Game.FixedDeltaTime;
                    }

                    if (released)
                    {
                        _tweenStart = Game.Now;
                        _selectPosition = position;
                    }

                    break;
                }
                else if (hook.IsEntityHovered(e.EntityId))
                {
                    // This entity is no longer being hovered.
                    hook.UnhoverEntity(e);
                }
                else if (_currentAreaRectangle.HasValue && _currentAreaRectangle.Value.Contains(position))
                {
                    // The entity is within a rectangle area.
                    hook.SelectEntity(e, clear: false);
                }
            }

            if (_dragTimer > DRAG_MIN_DURATION && _dragging != null)
            {
                Vector2 delta = cursorPosition - _dragging.GetGlobalTransform().Vector2 + _offset;

                // On "ctrl", snap entities to the grid.
                bool snapToGrid = Game.Input.Down(MurderInputButtons.Ctrl);

                // Drag all the entities which are currently selected.
                foreach ((int _, Entity e) in selectedEntities)
                {
                    if (e.IsDestroyed)
                    {
                        hook.UnselectEntity(e);
                        continue;
                    }

                    IMurderTransformComponent newTransform = e.GetGlobalTransform().Add(delta);
                    if (snapToGrid)
                    {
                        newTransform = newTransform.SnapToGridDelta();
                    }

                    e.SetGlobalTransform(newTransform);
                }
            }

            if (!Game.Input.Down(MurderInputButtons.LeftClick))
            {
                // The user stopped clicking, so no longer drag anything.
                _dragTimer = 0;
                _dragging = null;
            }

            if (hasFocus && clicked && !clickedOnEntity)
            {
                if (!_isShowingImgui)
                {
                    // User clicked in an empty space (which is not targeting any entities).
                    hook.UnselectAll();
                }

                // We might have just started a group!
                _startedGroupInWorld = cursorPosition;
                _currentAreaRectangle = GridHelper.FromTopLeftToBottomRight(_startedGroupInWorld.Value, cursorPosition);
            }

            if (_dragTimer > DRAG_MIN_DURATION)
            {
                hook.Cursor = CursorStyle.Hand;
            }

            if (_startedGroupInWorld != null && _currentAreaRectangle != null)
            {
                if (!released)
                {
                    Rectangle target = GridHelper.FromTopLeftToBottomRight(_startedGroupInWorld.Value, cursorPosition);
                    _currentAreaRectangle = Rectangle.Lerp(_currentAreaRectangle.Value, target, 0.45f);
                }
                else
                {
                    // Reset our group and wrap it up.
                    _startedGroupInWorld = null;
                    _currentAreaRectangle = null;

                    _dragTimer = DRAG_MIN_DURATION + 1;
                }
            }

            if (_currentAreaRectangle.HasValue)
            {
                hook.SelectionBox = _currentAreaRectangle.Value;
            }
        }

        /// <summary>
        /// Tracks tween for <see cref="_selectPosition"/>.
        /// </summary>
        private float _tweenStart;

        /// <summary>
        /// This is the position currently selected by the cursor.
        /// </summary>
        private Vector2? _selectPosition;

        private readonly Color _hoverColor = (Game.Profile.Theme.Accent * .7f);

        protected void DrawImpl(RenderContext render, World world, ImmutableArray<Entity> entities)
        {
            EditorHook hook = world.GetUnique<EditorComponent>().EditorHook;
            foreach (Entity e in entities)
            {
                if (!e.HasTransform()) continue;

                Vector2 position = e.GetGlobalTransform().Vector2;
                if (hook.IsEntityHovered(e.EntityId))
                {
                    Rectangle hoverRectangle = new(position - _selectionBox / 2f, _selectionBox);
                    RenderServices.DrawRectangleOutline(render.DebugSpriteBatch, hoverRectangle, _hoverColor);
                }
                else if (hook.IsEntitySelected(e.EntityId))
                {
                    Rectangle hoverRectangle = new(position - _selectionBox / 2f + _selectionBox / 4f, _selectionBox / 2f);
                    RenderServices.DrawRectangleOutline(render.DebugSpriteBatch, hoverRectangle, Game.Profile.Theme.Accent);
                }
                else
                {
                    var distance = (position - hook.CursorWorldPosition).Length() / 128f * render.Camera.Zoom;
                    if (distance < 1)
                    {
                        RenderServices.DrawCircle(render.DebugSpriteBatch, position, 2, 6, Game.Profile.Theme.Yellow * (1 - distance));
                    }
                }
            }

            DrawSelectionTween(render);
            DrawSelectionRectangle(render);
        }

        private void DrawSelectionTween(RenderContext render)
        {
            if (_selectPosition is Vector2 position)
            {
                float tween = Ease.ZeroToOne(Ease.BackOut, 2f, _tweenStart);
                if (tween == 1)
                {
                    _selectPosition = null;
                }
                else
                {
                    float expand = (1 - tween) * 3;

                    float startAlpha = .9f;
                    Color color = Game.Profile.Theme.Accent * (startAlpha - startAlpha * tween);

                    Vector2 size = _selectionBox + expand * 2;
                    Rectangle rectangle = new(position - size / 2f, size);

                    RenderServices.DrawRectangleOutline(render.DebugSpriteBatch, rectangle, color);
                }
            }
        }

        private void DrawSelectionRectangle(RenderContext render)
        {
            if (_currentAreaRectangle is not null && _currentAreaRectangle.Value.Size.X > 1)
            {
                RenderServices.DrawRectangle(render.DebugFxSpriteBatch, _currentAreaRectangle.Value, Color.White * .25f);
                RenderServices.DrawRectangleOutline(render.DebugFxSpriteBatch, _currentAreaRectangle.Value, Color.White * .75f);
            }
        }

        private void OnEntityToggled(Entity e, bool selected)
        {
            if (selected)
            {
                e.AddOrReplaceComponent(new IsSelectedComponent());
            }
            else
            {
                e.RemoveComponent<IsSelectedComponent>();
            }
        }
    }
}