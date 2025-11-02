using System;
using System.Runtime.InteropServices;

namespace AutoDragonOath.Models
{
    /// <summary>
    /// Represents a character title in Dragon Oath
    /// Based on _TITLE_ structure from game source code
    /// </summary>
    public class CharacterTitle
    {
        /// <summary>
        /// Title type flags
        /// </summary>
        public enum TitleFlag
        {
            INVALID_TITLE = 0,
            ID_TITLE = 1,      // Title is stored as an ID (lookup in database)
            STRING_TITLE = 2   // Title is stored as a string
        }

        /// <summary>
        /// Title type categories
        /// </summary>
        public enum TitleType : byte
        {
            NO_TITLE = 0,
            // Add more types as discovered
        }

        public TitleFlag Flag { get; set; }
        public TitleType Type { get; set; }
        public int TitleID { get; set; }
        public string TitleString { get; set; }

        /// <summary>
        /// Get display text for this title
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (Flag == TitleFlag.INVALID_TITLE)
                    return "";

                if (Flag == TitleFlag.STRING_TITLE && !string.IsNullOrEmpty(TitleString))
                    return TitleString;

                if (Flag == TitleFlag.ID_TITLE && TitleID > 0)
                    return $"[Title ID: {TitleID}]";

                return "";
            }
        }

        public bool IsValid => Flag != TitleFlag.INVALID_TITLE;

        public override string ToString()
        {
            if (!IsValid)
                return "(No Title)";

            return Flag == TitleFlag.STRING_TITLE
                ? $"String: \"{TitleString}\""
                : $"ID: {TitleID}";
        }
    }

    /// <summary>
    /// Title list for a character
    /// Based on SDATA_PLAYER_OTHER structure
    /// </summary>
    public class CharacterTitles
    {
        public const int MAX_TITLE_SIZE = 20;

        public int TitleCount { get; set; }
        public int CurrentTitleIndex { get; set; }
        public CharacterTitle[] Titles { get; set; }

        public CharacterTitles()
        {
            Titles = new CharacterTitle[MAX_TITLE_SIZE];
            for (int i = 0; i < MAX_TITLE_SIZE; i++)
            {
                Titles[i] = new CharacterTitle();
            }
        }

        /// <summary>
        /// Get the currently active title
        /// </summary>
        public CharacterTitle CurrentTitle
        {
            get
            {
                if (CurrentTitleIndex >= 0 && CurrentTitleIndex < TitleCount)
                    return Titles[CurrentTitleIndex];
                return new CharacterTitle();
            }
        }

        /// <summary>
        /// Get all valid titles
        /// </summary>
        public CharacterTitle[] GetValidTitles()
        {
            var validTitles = new System.Collections.Generic.List<CharacterTitle>();
            for (int i = 0; i < TitleCount && i < MAX_TITLE_SIZE; i++)
            {
                if (Titles[i].IsValid)
                    validTitles.Add(Titles[i]);
            }
            return validTitles.ToArray();
        }

        public override string ToString()
        {
            return $"Titles: {TitleCount}/{MAX_TITLE_SIZE}, Current: {CurrentTitle.DisplayText}";
        }
    }
}
