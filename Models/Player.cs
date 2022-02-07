using System;

namespace RFPlayerLibrary.Models
{
    public class Player
    {
        public long Id { get; set; }
        public ulong SteamId { get; set; }
        public ulong SteamGroupId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool Gold { get; set; }
        public byte Beard { get; set; }
        public byte Face { get; set; }
        public byte Hair { get; set; }
        public string Skin { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int BackpackSkin { get; set; }
        public int GlassesSkin { get; set; }
        public int HatSkin { get; set; }
        public int MaskSkin { get; set; }
        public int PantsSkin { get; set; }
        public int ShirtSkin { get; set; }
        public int VestSkin { get; set; }
        public string ItemSkins { get; set; } = string.Empty;
        public string SkinTags { get; set; } = string.Empty;
        public string SkinDynamicProps { get; set; } = string.Empty;
        public string SelectedSkillset { get; set; } = string.Empty;
        public DateTime ConnectedTime { get; set; } = DateTime.Now;
        public DateTime DisconnectedTime { get; set; } = DateTime.Now;
        public ulong PlayTime { get; set; }
        public string HWID { get; set; } = string.Empty;

        public Player()
        {
            
        }
        
        public Player Copy()
        {
            return (Player) MemberwiseClone();
        }
    }
}