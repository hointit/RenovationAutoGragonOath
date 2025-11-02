using AutoDragonOath.Models;
using System;
using System.Text;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Reads character title data from game memory
    /// Implements reading of _TITLE_ structure and title list
    /// </summary>
    public class TitleReader
    {
        private readonly MemoryReader _memoryReader;

        // Structure sizes (based on game source code)
        private const int TITLE_STRUCTURE_SIZE = 44;  // sizeof(_TITLE_) with padding
        private const int MAX_CHARACTER_TITLE = 34;
        private const int MAX_TITLE_SIZE = 20;

        // Offsets within _TITLE_ structure
        private const int OFFSET_FLAG = 0;      // BOOL (4 bytes)
        private const int OFFSET_TYPE = 4;      // BYTE (1 byte)
        // 3 bytes padding
        private const int OFFSET_UNION = 8;     // INT (4) or CHAR[34]

        public TitleReader(int processId)
        {
            _memoryReader = new MemoryReader(processId);
        }

        public TitleReader(MemoryReader memoryReader)
        {
            _memoryReader = memoryReader;
        }

        /// <summary>
        /// Read all titles for the character
        /// NOTE: This requires finding the SDATA_PLAYER_OTHER structure offset
        /// which is NOT yet confirmed in the current memory mappings
        /// </summary>
        public CharacterTitles ReadTitles(int statsBaseAddress)
        {
            var titles = new CharacterTitles();

            try
            {
                // IMPORTANT: These offsets are ESTIMATES and need verification!
                // The SDATA_PLAYER_OTHER structure comes BEFORE SDATA_PLAYER_MYSELF in memory
                // We need to find the actual offset by scanning or analysis

                // For now, we'll document how to read them when the offset is found
                // Estimated structure layout (NEEDS CONFIRMATION):
                // SDATA_CHARACTER fields
                // SDATA_NPC fields
                // SDATA_PLAYER_OTHER fields:
                //   INT m_nMenPai
                //   INT m_nHairMeshID, m_nHairMaterialID
                //   INT m_nFaceMeshID, m_nFaceMaterialID
                //   INT m_nEquipVer
                //   INT m_nEquipmentID[HEQUIP_NUMBER]
                //   BOOL m_bTeamFlag, m_bTeamLeaderFlag, m_bTeamFullFlag
                //   INT m_nTitleNum  ← UNKNOWN OFFSET
                //   INT m_nCurTitleIndex
                //   _TITLE_ m_nTitleList[20]

                // Placeholder - returns empty titles until offsets are found
                System.Diagnostics.Debug.WriteLine("WARNING: Title reading not implemented - offsets unknown");

                return titles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading titles: {ex.Message}");
                return titles;
            }
        }

        /// <summary>
        /// Read title count and current index (when offset is known)
        /// </summary>
        public (int count, int currentIndex) ReadTitleInfo(int titleInfoBaseAddress)
        {
            try
            {
                int titleNum = _memoryReader.ReadInt32(titleInfoBaseAddress);
                int currentIndex = _memoryReader.ReadInt32(titleInfoBaseAddress + 4);

                return (titleNum, currentIndex);
            }
            catch
            {
                return (0, -1);
            }
        }

        /// <summary>
        /// Read a single title from memory
        /// </summary>
        /// <param name="titleAddress">Base address of the _TITLE_ structure</param>
        public CharacterTitle ReadTitle(int titleAddress)
        {
            var title = new CharacterTitle();

            try
            {
                // Read bFlag (BOOL, 4 bytes)
                int flag = _memoryReader.ReadInt32(titleAddress + OFFSET_FLAG);
                title.Flag = (CharacterTitle.TitleFlag)flag;

                // Read bType (BYTE, 1 byte)
                byte type = (byte)_memoryReader.ReadInt32(titleAddress + OFFSET_TYPE);
                title.Type = (CharacterTitle.TitleType)type;

                // Read union data
                if (title.Flag == CharacterTitle.TitleFlag.ID_TITLE)
                {
                    // Read as INT
                    title.TitleID = _memoryReader.ReadInt32(titleAddress + OFFSET_UNION);
                }
                else if (title.Flag == CharacterTitle.TitleFlag.STRING_TITLE)
                {
                    // Read as string
                    title.TitleString = _memoryReader.ReadString(titleAddress + OFFSET_UNION, MAX_CHARACTER_TITLE);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading title at 0x{titleAddress:X8}: {ex.Message}");
            }

            return title;
        }

        /// <summary>
        /// Read title list array from memory (when offset is known)
        /// </summary>
        public CharacterTitle[] ReadTitleList(int titleListBaseAddress)
        {
            var titles = new CharacterTitle[MAX_TITLE_SIZE];

            for (int i = 0; i < MAX_TITLE_SIZE; i++)
            {
                int titleAddress = titleListBaseAddress + (i * TITLE_STRUCTURE_SIZE);
                titles[i] = ReadTitle(titleAddress);
            }

            return titles;
        }

        /// <summary>
        /// Scan memory to find title structure offsets
        /// This is a helper function to locate where titles are stored
        /// </summary>
        public void ScanForTitleStructure(int statsBaseAddress, int scanRange = 5000)
        {
            System.Diagnostics.Debug.WriteLine("=== Scanning for Title Structure ===\n");

            // Try to find patterns that indicate title data
            // Look for m_nTitleNum (reasonable values: 0-20)
            // Followed by m_nCurTitleIndex (reasonable values: -1 to 19)

            for (int offset = 0; offset < scanRange; offset += 4)
            {
                try
                {
                    int titleNum = _memoryReader.ReadInt32(statsBaseAddress + offset);
                    int currentIndex = _memoryReader.ReadInt32(statsBaseAddress + offset + 4);

                    // Check if values are reasonable
                    if (titleNum >= 0 && titleNum <= MAX_TITLE_SIZE &&
                        currentIndex >= -1 && currentIndex < MAX_TITLE_SIZE)
                    {
                        System.Diagnostics.Debug.WriteLine($"Potential title info at offset +{offset}:");
                        System.Diagnostics.Debug.WriteLine($"  TitleNum: {titleNum}");
                        System.Diagnostics.Debug.WriteLine($"  CurrentIndex: {currentIndex}");

                        // Try to read first title
                        int firstTitleAddr = statsBaseAddress + offset + 8;
                        int firstTitleFlag = _memoryReader.ReadInt32(firstTitleAddr);

                        if (firstTitleFlag >= 0 && firstTitleFlag <= 2)
                        {
                            System.Diagnostics.Debug.WriteLine($"  First title flag: {firstTitleFlag}");
                            System.Diagnostics.Debug.WriteLine($"  → POSSIBLE MATCH at offset +{offset}\n");
                        }
                    }
                }
                catch
                {
                    // Skip invalid addresses
                }
            }
        }
    }
}
