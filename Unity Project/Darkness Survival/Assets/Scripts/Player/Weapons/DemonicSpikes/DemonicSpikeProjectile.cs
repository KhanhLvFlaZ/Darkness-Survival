using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DemonicSpikeProjectile : MonoBehaviour
{
    Vector3 direction;
    [SerializeField] float speed;
    [SerializeField] GameObject spikeObject;
    [SerializeField] GameObject spikeExplosionPrefab;
    Collider2D spikeCollider;
    Transform ownerTransform;
    bool ownerIsMonster;

    float damage;

    private void Start()
    {
        spikeCollider = GetComponent<BoxCollider2D>();
        if (spikeCollider == null)
        {
            spikeCollider = gameObject.AddComponent<BoxCollider2D>();
            spikeCollider.isTrigger = true;
        }
    }

    public void Initialize(Transform owner)
    {
        ownerTransform = owner;
        ownerIsMonster = ownerTransform != null && ownerTransform.GetComponentInParent<Monsters>() != null;
    }

    public void SetDirection(float dir_x, float dir_y)
    {
        direction = new Vector3(dir_x, dir_y).normalized;

        // Angle between X axis 
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Rotation
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Explosion direction 

        if (spikeExplosionPrefab != null && direction.x < 0f)
        {
            Vector3 newScale = spikeExplosionPrefab.transform.localScale;
            newScale.x *= -1;
            spikeExplosionPrefab.transform.localScale = newScale;
        }
    }


    private void Update()
    {
        transform.position += speed * Time.deltaTime * direction;

        if (spikeCollider == null)
        {
            return;
        }

        if (Time.frameCount % 6 == 0)
        {
            Collider2D[] hit = Physics2D.OverlapBoxAll(spikeCollider.bounds.center, spikeCollider.bounds.size, 0f);

            foreach (Collider2D c in hit)
            {
                if (ownerTransform != null && c.transform.IsChildOf(ownerTransform))
                {
                    continue;
                }

                if (ownerIsMonster && c.GetComponentInParent<Monsters>() != null)
                {
                    continue;
                }

                if (!ownerIsMonster && c.GetComponentInParent<Character>() != null)
                {
                    continue;
                }

                IDamageable damageableObj = c.GetComponentInParent<IDamageable>();

                if (damageableObj != null)
                {
                    damageableObj.TakeDamage(damage);

                    if (spikeExplosionPrefab != null)
                    {
                        GameObject spikeExplosion = Instantiate(spikeExplosionPrefab);

                        float offsetX = direction.x >= 0 ? 0.5f : -0.5f;

                        spikeExplosion.transform.position = new Vector3(
                            spikeObject.transform.position.x + offsetX,
                            spikeObject.transform.position.y,
                            spikeObject.transform.position.z);
                    }

                    Destroy(gameObject);

                    break;
                }
            }
        }
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }
}