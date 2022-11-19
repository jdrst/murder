using ImGuiNET;
using System.Collections.Immutable;
using Murder.ImGuiExtended;
using Murder.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Murder.Editor.ImGuiExtended;
using Murder.Prefabs;
using System.Linq;
using Murder.Core.Dialogs;
using System.Threading.Channels;
using System.Security.Cryptography;

namespace Murder.Editor.CustomEditors
{
    internal partial class WorldAssetEditor
    {
        private void DrawEntitiesEditor()
        {
            GameLogger.Verify(_asset is not null && Stages.ContainsKey(_asset.Guid));

            const string popupName = "New group";
            if (ImGuiHelpers.IconButton('\uf65e', $"add_group"))
            {
                _groupName = string.Empty;
                ImGui.OpenPopup(popupName);
            }

            DrawCreateOrRenameGroupPopup(popupName);

            ImGui.SameLine();
            if (CanAddInstance && ImGuiHelpers.IconButton('\uf234', $"add_entity_world"))
            {
                ImGui.OpenPopup("Add entity");
            }

            if (ImGui.BeginPopup("Add entity"))
            {
                if (SearchBox.SearchInstantiableEntities() is Guid asset)
                {
                    EntityInstance instance = EntityBuilder.CreateInstance(asset);
                    AddInstance(instance);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            _selecting = -1;

            DrawEntityGroups();
            
            if (TreeEntityGroupNode("All Entities", Game.Profile.Theme.White, icon: '\uf500', flags: ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawEntityList(group: null, Instances);

                ImGui.TreePop();
            }
        }
        
        /// <summary>
        /// Draw all folders with entities. Returns a list with all the entities grouped within those folders.
        /// </summary>
        protected virtual void DrawEntityGroups()
        {
            ImmutableDictionary<string, ImmutableArray<Guid>> folders = _world?.FetchFolders() ?? 
                ImmutableDictionary<string, ImmutableArray<Guid>>.Empty;
            
            foreach ((string name, ImmutableArray<Guid> entities) in folders)
            {
                if (TreeEntityGroupNode(name, Game.Profile.Theme.Yellow))
                {
                    if (ImGuiHelpers.DeleteButton($"Delete_group_{name}"))
                    {
                        DeleteGroupWithEntities(name);
                    }

                    ImGui.SameLine();

                    string popupName = $"Rename group##{name}";
                    if (ImGuiHelpers.IconButton('\uf304', $"rename_group_{name}"))
                    {
                        _groupName = name;
                        
                        ImGui.OpenPopup(popupName);
                    }

                    // TODO: Implement locking entities per group...
                    //ImGui.SameLine();
                    //if (ImGuiHelpers.IconButton('\uf023', $"lock_group_{name}"))
                    //{

                    //}

                    DrawCreateOrRenameGroupPopup(popupName, previousName: name);

                    DrawEntityList(name, entities);
                    ImGui.TreePop();
                }
            }
        }

        private bool TreeEntityGroupNode(string name, System.Numerics.Vector4 textColor, char icon = '\ue1b0', ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None) =>
            ImGuiHelpers.TreeNodeWithIconAndColor(
                icon: icon,
                label: name,
                flags: ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.FramePadding | flags,
                text: textColor, 
                background: Game.Profile.Theme.BgFaded, 
                active: Game.Profile.Theme.Bg);

        /// <summary>
        /// Draw the entity list of <paramref name="group"/>.
        /// </summary>
        /// <param name="group">Group unique name. None if it doesn't belong to any.</param>
        /// <param name="entities">Entities that belong to the group.</param>
        private void DrawEntityList(string? group, IList<Guid> entities)
        {
            GameLogger.Verify(_asset is not null);
            
            if (entities.Count == 0)
            {
                ImGui.TextColored(Game.Profile.Theme.Faded, "Drag an entity here!");

                if (DragDrop<Guid>.DragDropTarget($"drag_instance", out Guid draggedId))
                {
                    _world?.MoveToGroup(group, draggedId, 0);
                }
            }

            for (int i = 0; i < entities.Count; ++i)
            {
                Guid entity = entities[i];
                
                // If we are showing all entities (group is null), only show entities that does not belong to any group.
                if (group is null && _world is not null && _world.BelongsToAnyGroup(entity))
                {
                    continue;
                }

                if (ImGuiHelpers.DeleteButton($"Delete_{entity}"))
                {
                    DeleteInstance(parent: null, entity);
                    
                    continue;
                }

                ImGui.SameLine();

                ImGui.PushID($"Entity_bar_{entity}");

                bool isSelected = Stages[_asset.Guid].IsSelected(entity);

                string? name = TryFindInstance(entity)?.Name;
                if (ImGui.Selectable(name ?? "<?>", isSelected))
                {
                    _selecting = Stages[_asset.Guid].SelectEntity(entity, select: true);
                    if (_selecting is -1)
                    {
                        // Unable to find the entity. This probably means that it has been deactivated.
                        _selectedAsset = entity;
                    }
                    else
                    {
                        _selectedAsset = null;
                    }
                }

                DragDrop<Guid>.DragDropSource("drag_instance", name ?? "instance", entity);

                ImGui.PopID();

                if (DragDrop<Guid>.DragDropTarget($"drag_instance", out Guid draggedId))
                {
                    _world?.MoveToGroup(group, draggedId, i);
                }
            }
        }
        
        private string _groupName = "";

        private void DrawCreateOrRenameGroupPopup(string popupName, string? previousName = null)
        {
            if (_world is null)
            {
                GameLogger.Warning("Unable to add group without a world!");
                return;
            }

            bool isRename = previousName is not null;

            if (ImGui.BeginPopup(popupName))
            {
                if (!isRename)
                {
                    ImGui.Text("What's the new group name?");
                }
                
                ImGui.InputText("##group_name", ref _groupName, 128, ImGuiInputTextFlags.AutoSelectAll);

                string buttonLabel = isRename ? "Rename" : "Create";
                
                if (!string.IsNullOrWhiteSpace(_groupName) && !_world.FetchFolders().ContainsKey(_groupName))
                {
                    if (ImGui.Button(buttonLabel) || Game.Input.Pressed(Keys.Enter))
                    {
                        if (previousName is not null)
                        {
                            _world.RenameGroup(previousName, _groupName);
                        }
                        else
                        {
                            AddGroup(_groupName);
                        }

                        ImGui.CloseCurrentPopup();
                    }
                }
                else
                {
                    ImGuiHelpers.DisabledButton(buttonLabel);
                    ImGuiHelpers.HelpTooltip("Unable to add group with duplicate or empty names.");
                }

                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// Add a group to the world. This returns the valid name that it was able to create for this group.
        /// </summary>
        private string? AddGroup(string name)
        {
            if (_world is null)
            {
                GameLogger.Warning("Unable to add group without a world!");
                return null;
            }

            if (_world.HasGroup(name))
            {
                name += $" {_world.GroupsCount()}";

                return AddGroup(name);
            }

            _world.AddGroup(name);
            return name;
        }
    }
}