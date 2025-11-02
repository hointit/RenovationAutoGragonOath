using AutoDragonOath.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Service for monitoring game processes and reading character information
    /// Combines functionality from Class0.cs and GClass0.cs
    /// </summary>
    public class GameProcessMonitor
    {
        private const string GAME_PROCESS_NAME = "Game";
        private static readonly int[] EntityBasePointer = { 2381824, 12 };
        private static readonly int[] StatsBasePointer = { 2381824, 12, 340, 4 };
        private const int OFFSET_CHARACTER_NAME = 48;
        private const int OFFSET_CURRENT_MP = 1756;
        private const int OFFSET_LEVEL = 92;
        private const int OFFSET_PET_ID = 2356;
        private const int OFFSET_CURRENT_HP = 1752;
        private const int OFFSET_MAX_HP = OFFSET_CURRENT_MP + 100;
        private const int OFFSET_MAX_MP = OFFSET_CURRENT_MP + 104;
        private const int OFFSET_EXPERIENCE = 2408;  // Experience points offset
        private const int OFFSET_CURRENT_MAP = 88;
        private const int OFFSET_X_COORDINATE = 92;
        private const int OFFSET_Y_COORDINATE = 100;
        
        // Offsets from map base
        private static readonly int[] MapBasePointer = { 2381900 };
        private const int OFFSET_MAP_ID = 380;

        // Pet information base
        private static readonly int[] PetBasePointer = { 7319540, 299356 };
        private const int PET_ENTRY_SIZE = 92; // Size of each pet entry
        private const int OFFSET_PET_CURRENT_HP = 40;
        private const int OFFSET_PET_MAX_HP = 44;
        private const int OFFSET_PET_ID_CHECK = 36;

        /// <summary>
        /// Scan for all running game processes
        /// </summary>
        public List<CharacterInfo> ScanForGameProcesses()
        {
            var characters = new List<CharacterInfo>();

            try
            {
                var processes = Process.GetProcessesByName(GAME_PROCESS_NAME);

                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Responding)
                        {
                            var characterInfo = ReadCharacterInfo(process.Id);
                            if (characterInfo != null)
                            {
                                characters.Add(characterInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading process {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning for processes: {ex.Message}");
            }

            return characters;
        }

        /// <summary>
        /// Read character information from a specific process
        /// </summary>
        public CharacterInfo? ReadCharacterInfo(int processId)
        {
            try
            {
                using var memoryReader = new MemoryReader(processId);

                // Check if process handle is valid
                if (!memoryReader.IsValid)
                {
                    Debug.WriteLine($"Failed to open process {processId} - may need Administrator rights");
                    return null;
                }

                // Get stats base address
                int statsBase = memoryReader.FollowPointerChain(StatsBasePointer);
                if (statsBase == 0)
                {
                    Debug.WriteLine($"Failed to read stats base for process {processId}");
                    return null;
                }

                // Get entity base address
                int entityBase = memoryReader.FollowPointerChain(EntityBasePointer);

                var characterInfo = new CharacterInfo
                {
                    ProcessId = processId
                };

                // Read character name
                characterInfo.CharacterName = memoryReader.ReadString(statsBase + OFFSET_CHARACTER_NAME);

                // Read level
                characterInfo.Level = memoryReader.ReadInt32(statsBase + OFFSET_LEVEL);

                // Read HP
                int currentHp = memoryReader.ReadInt32(statsBase + OFFSET_CURRENT_HP);
                int maxHp = memoryReader.ReadInt32(statsBase + OFFSET_MAX_HP);
                characterInfo.HpPercent = maxHp > 0 ? (int)((float)currentHp * 100 / maxHp) : 100;

                // Read MP
                int currentMp = memoryReader.ReadInt32(statsBase + OFFSET_CURRENT_MP);
                int maxMp = memoryReader.ReadInt32(statsBase + OFFSET_MAX_MP);
                characterInfo.MpPercent = maxMp > 0 ? (int)((float)currentMp * 100 / maxMp) : 100;

                // Read experience
                characterInfo.Experience = memoryReader.ReadInt32(statsBase + OFFSET_EXPERIENCE);

                // Read coordinates
                if (entityBase != 0)
                {
                    characterInfo.XCoordinate = (int)memoryReader.ReadFloat(entityBase + OFFSET_X_COORDINATE);
                    characterInfo.YCoordinate = (int)memoryReader.ReadFloat(entityBase + OFFSET_Y_COORDINATE);
                }

                // Read map
                int mapBase = memoryReader.FollowPointerChain(MapBasePointer);
                if (mapBase != 0)
                {
                    int mapId = memoryReader.ReadInt32(mapBase + OFFSET_MAP_ID);
                    characterInfo.MapName = mapId.ToString();
                }

                // Read pet HP
                int petId = memoryReader.ReadInt32(statsBase + OFFSET_PET_ID);
                if (petId > 0)
                {
                    characterInfo.PetHpPercent = ReadPetHp(memoryReader, petId);
                }

                // Read skills (F1-F12 placeholders - actual skill names not in memory)
                characterInfo.Skills = GetSkillPlaceholders();

                // Read titles (if offset is known)
                // NOTE: The TITLE_INFO_OFFSET is currently unknown
                // To find it, use: new TitleReader(memoryReader).ScanForTitleStructure(statsBase, 5000);
                // Once found, uncomment the code below and set TITLE_INFO_OFFSET constant
                /*
                if (TITLE_INFO_OFFSET > 0)
                {
                    try
                    {
                        var titleReader = new TitleReader(memoryReader);
                        int titleInfoAddr = statsBase + TITLE_INFO_OFFSET;

                        var (count, currentIdx) = titleReader.ReadTitleInfo(titleInfoAddr);
                        characterInfo.TitleCount = count;

                        if (currentIdx >= 0 && currentIdx < count)
                        {
                            int titleListAddr = titleInfoAddr + 8;  // Title list starts 8 bytes after (count + currentIndex)
                            var titles = titleReader.ReadTitleList(titleListAddr);
                            characterInfo.CurrentTitle = titles[currentIdx].DisplayText;
                        }
                        else
                        {
                            characterInfo.CurrentTitle = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading titles: {ex.Message}");
                    }
                }
                */

                return characterInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading character info for process {processId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Read pet HP percentage
        /// </summary>
        private int ReadPetHp(MemoryReader memoryReader, int petId)
        {
            try
            {
                int petBase = memoryReader.FollowPointerChain(PetBasePointer);
                if (petBase == 0)
                    return 0;

                // Search for pet in array (max 20 pets)
                for (int i = 0; i < 20; i++)
                {
                    int petIdCheck = memoryReader.ReadInt32(petBase + i * PET_ENTRY_SIZE + OFFSET_PET_ID_CHECK);
                    if (petIdCheck == petId)
                    {
                        int currentHp = memoryReader.ReadInt32(petBase + i * PET_ENTRY_SIZE + OFFSET_PET_CURRENT_HP);
                        int maxHp = memoryReader.ReadInt32(petBase + i * PET_ENTRY_SIZE + OFFSET_PET_MAX_HP);

                        if (maxHp > 0)
                            return (int)((float)currentHp / maxHp * 100);

                        return 0;
                    }

                    if (petIdCheck == 0)
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading pet HP: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Convert map ID to readable map name
        /// From GClass3.smethod_0 and various checks in GClass0.cs
        /// </summary>
        private string ConvertMapIdToName(int mapId)
        {
            return mapId switch
            {
                1 => "Heaven",
                10 => "Nhon Nam",
                11 => "Loc Duong",
                20 => "Don Hoang",
                21 => "Kiem Cac",
                30 => "Doi Lu",
                31 => "Nhi Hai",
                37 => "Bang Thien",
                40 => "Ta Lu",
                50 => "Dai Li",
                60 => "My Nhan",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get placeholder skills (F1-F12)
        /// Note: The original code doesn't read skill names from memory
        /// This provides F1-F12 placeholders that can be customized
        /// </summary>
        private List<SkillInfo> GetSkillPlaceholders()
        {
            return new List<SkillInfo>
            {
                new SkillInfo("Skill 1", "F1"),
                new SkillInfo("Skill 2", "F2"),
                new SkillInfo("Skill 3", "F3"),
                new SkillInfo("Skill 4", "F4"),
                new SkillInfo("Skill 5", "F5"),
                new SkillInfo("Skill 6", "F6"),
                new SkillInfo("Skill 7", "F7"),
                new SkillInfo("Skill 8", "F8"),
                new SkillInfo("Skill 9", "F9"),
                new SkillInfo("Skill 10", "F10"),
                new SkillInfo("Skill 11", "F11"),
                new SkillInfo("Skill 12", "F12")
            };
        }
    }
}
