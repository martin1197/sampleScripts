using UnityEngine;

namespace Entity
{
    public class CategoryEntity
    {
        public string Id;
        public string IdParent;
        public string IsLeaf;
        public string Name;
        public CategoryEntity[] Children;
        public Sprite Image;
    }
}