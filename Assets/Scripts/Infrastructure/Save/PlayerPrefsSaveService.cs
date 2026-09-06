using UnityEngine;

namespace Infrastructure.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public int LoadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }
    }
}