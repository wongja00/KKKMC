using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BulletDecal : MonoBehaviour
{
    [SerializeField] List<Sprite> sprites;

    [SerializeField] SpriteRenderer material;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDecalImage()
    {
        //if (sprites != null && sprites.Count > 0 && material != null)
        //{
        //    int randIndex = UnityEngine.Random.Range(0, sprites.Count);
//
        //    Sprite randomSprite = sprites[randIndex];
        //    
        //    if (randomSprite != null)
        //    {
        //        material.sprite = randomSprite;
        //    }
        //}

        StartCoroutine(DeleteBullet());
    }

    public IEnumerator DeleteBullet()
    {
        yield return new WaitForSeconds(5f);
        Destroy(this.gameObject);
    }
}
