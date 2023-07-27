using UnityEngine;

namespace Behaviours
{
    public interface IColliderInfo
    {
        Vector2 Size { get; set; }
        Vector2 Offset { get; set; }

        Collider2D Collider { get; }
    }

    public abstract class ColliderInfoFactory
    {
        public static IColliderInfo NewColliderInfo(Collider2D collider)
        {
            return collider switch
            {
                CapsuleCollider2D collider2D => new CapsuleColliderInfo(collider2D),
                BoxCollider2D boxCollider2D => new BoxColliderInfo(boxCollider2D),
                _ => throw new System.Exception("No ColliderInfo implementation for type: " +
                                                (collider != null ? collider.name : "NULL"))
            };
        }
    }

    public class CapsuleColliderInfo : IColliderInfo
    {
        private readonly CapsuleCollider2D _capsuleCollider;
        
        public CapsuleColliderInfo(CapsuleCollider2D inCapsuleCollider)
        {
            _capsuleCollider = inCapsuleCollider;
        }

        public Vector2 Size 
        { 
            get => _capsuleCollider.size; 
            set => _capsuleCollider.size = value; 
        }

        public Vector2 Offset 
        { 
            get => _capsuleCollider.offset; 
            set => _capsuleCollider.offset = value; 
        }

        public Collider2D Collider => _capsuleCollider;
    }

    public class BoxColliderInfo : IColliderInfo
    {
        private readonly BoxCollider2D _boxCollider;
        
        public BoxColliderInfo(BoxCollider2D inBoxCollider)
        {
            _boxCollider = inBoxCollider;
        }

        
        public Vector2 Size
        {
            get => _boxCollider.size;
            set => _boxCollider.size = value;
        }

        public Vector2 Offset 
        { 
            get => _boxCollider.offset; 
            set => _boxCollider.offset = value; 
        }

        public Collider2D Collider => _boxCollider;
    }
}