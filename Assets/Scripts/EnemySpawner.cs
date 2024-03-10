
using System.Collections;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    public Transform targetPos;
    
    private void Update() {
        
        if(WaveManager.instance.coroutineControl){
            StartCoroutine(SpawnEnemy());
        }
    }
    IEnumerator SpawnEnemy(){
        WaveManager.instance.coroutineControl = false;
        WaitForSeconds spawnDelay = new WaitForSeconds(WaveManager.instance.spawnDelay);
        int spawn = 0;
        while(WaveManager.instance.enemyToSpawn.Count > 0){
            spawn++;
            // print("spawn time: " + spawn);
            
            GameObject enemy = WaveManager.instance.enemyToSpawn[Random.Range(0,WaveManager.instance.enemyToSpawn.Count-1)];
            if(enemy != null){
                GameObject enemyClone =  Instantiate(enemy, transform.position, Quaternion.identity);
                enemyClone.GetComponent<EnemyMovement>().SetTargetPosition(targetPos);
                WaveManager.instance.enemySpawned.Add(enemyClone);
                WaveManager.instance.enemyToSpawn.Remove(enemy);
                yield return spawnDelay;
            }
        }
    }
}
