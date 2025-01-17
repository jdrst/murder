﻿using Murder.Diagnostics;
using Murder.Editor.Assets;
using Murder.Serialization;

namespace Murder.Editor.Importers
{
    [ImporterSettings(FilterType.OnlyTheseFolders, new string[] { "no_atlas" }, new string[] { ".png" })]
    internal class PngNoAtlasImporter : ResourceImporter
    {
        public override string RelativeSourcePath => "no_atlas";
        public override string RelativeOutputPath => "images";
        public override string RelativeDataOutputPath => string.Empty;

        internal override ValueTask LoadStagedContentAsync(EditorSettingsAsset editorSettings, bool forceAll)
        {
            string sourcePath = GetFullSourcePath(editorSettings);
            string outputPath = GetFullOutputPath(editorSettings);

            int skippedFiles = AllFiles.Count - ChangedFiles.Count;

            FileHelper.GetOrCreateDirectory(outputPath);

            if (!forceAll)
            {
                foreach (var image in ChangedFiles)
                {
                    CopyImage(sourcePath, outputPath, image);
                }

                if (skippedFiles > 0)
                {
                    GameLogger.Log($"Png(no-atlas) importer skipped {skippedFiles} files because they were not modified.");
                }
                
                if (ChangedFiles.Count > 0)
                {
                    CopyOutputToBin = true;
                }
            }
            else
            {
                // Cleanup folder for the new assets
                FileHelper.DeleteContent(outputPath, deleteRootFiles: true);
                
                foreach (var image in AllFiles)
                {
                    CopyImage(sourcePath, outputPath, image);
                }

                if (AllFiles.Count > 0)
                {
                    CopyOutputToBin = true;
                }
            }
            GameLogger.Log($"Png(no-atlas) importer loaded {ChangedFiles.Count} files.");

            return default;
        }

        private void CopyImage(string sourcePath, string outputPath, string image)
        {
            var target = Path.Join(outputPath, Path.GetRelativePath(sourcePath, image));
            File.Copy(image, target, true);

            if (Verbose)
            {
                GameLogger.Log($"Copied {image} to {target}");
            }
        }
    }
}
