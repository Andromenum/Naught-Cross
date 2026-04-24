using UnityEngine;

[CreateAssetMenu(fileName = "ProfileIconLibrary", menuName = "Game/Profile Icon Library")]
public class ProfileIconLibrary : ScriptableObject
{
    [SerializeField] private Sprite[] icons;

    public int Count => icons != null ? icons.Length : 0;

    public Sprite GetIcon(int index)
    {
        if (icons == null || icons.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, icons.Length - 1);
        return icons[index];
    }
}