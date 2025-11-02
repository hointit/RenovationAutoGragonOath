using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoDragonOath.Models;

namespace AutoDragonOath.Helpers
{
    /// <summary>
    /// Helper class for reading and managing scene data from JSON file
    /// </summary>
    public class SceneReader
    {
        private static SceneReader? _instance;
        private Dictionary<int, Scene> _scenesByClientRes;
        private List<Scene> _scenes;

        /// <summary>
        /// Singleton instance of SceneReader
        /// </summary>
        public static SceneReader Instance => _instance ??= new SceneReader();

        private SceneReader()
        {
            _scenesByClientRes = new Dictionary<int, Scene>();
            _scenes = new List<Scene>();
            LoadScenes();
        }

        /// <summary>
        /// Load scenes from the JSON file
        /// </summary>
        private void LoadScenes()
        {
            try
            {
                string scenePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Helpers", "scene.json");
                
                // If not found in output directory, try relative to project
                if (!File.Exists(scenePath))
                {
                    scenePath = Path.Combine(Directory.GetCurrentDirectory(), "Helpers", "scene.json");
                }

                if (!File.Exists(scenePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Scene file not found at: {scenePath}");
                    return;
                }

                string jsonContent = File.ReadAllText(scenePath);
                var scenes = JsonSerializer.Deserialize<List<Scene>>(jsonContent);

                if (scenes != null)
                {
                    _scenes = scenes;
                    
                    // Build dictionary for fast lookup by clientres
                    foreach (var scene in scenes)
                    {
                        if (scene.ClientRes > 0 && !string.IsNullOrEmpty(scene.Name))
                        {
                            _scenesByClientRes[scene.ClientRes] = scene;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {_scenesByClientRes.Count} scenes from JSON");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading scenes: {ex.Message}");
            }
        }

        /// <summary>
        /// Get scene name by client resource ID (MapId)
        /// </summary>
        /// <param name="clientRes">The clientres/MapId value</param>
        /// <returns>Scene name or "Unknown" if not found</returns>
        public string GetSceneNameById(int clientRes)
        {
            if (_scenesByClientRes.TryGetValue(clientRes, out var scene))
            {
                return scene.Name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Get scene by client resource ID
        /// </summary>
        /// <param name="clientRes">The clientres/MapId value</param>
        /// <returns>Scene object or null if not found</returns>
        public Scene? GetSceneByClientRes(int clientRes)
        {
            return _scenesByClientRes.TryGetValue(clientRes, out var scene) ? scene : null;
        }

        /// <summary>
        /// Get all scenes
        /// </summary>
        public List<Scene> GetAllScenes()
        {
            return _scenes.ToList();
        }

        /// <summary>
        /// Reload scenes from file
        /// </summary>
        public void Reload()
        {
            _scenesByClientRes.Clear();
            _scenes.Clear();
            LoadScenes();
        }
    }
}
