using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace NobleMirrorSample.Player
{
    /// <summary>
    /// アイテムを拾う
    /// という処理を司るスクリプト
    /// プレイヤーキャラにアタッチする事を想定しています
    /// </summary>
    public class RayCastPickUp : NetworkBehaviour
    {
        [SerializeField] private float distance = 1.0f;
        private int range = 2;
        private RaycastHit hit;
        [Header("手に持っているアイテム")]
        [SerializeField] public GameObject pickedItemGameObject;
        private NetworkIdentity objNetId;

        private int layerMask = 1 << 9; //pickUpLayer is 9

        private int pickUpLayer = 9;
        private int weaponLayer = 10; //WeaponUpLayer is 10

        [SerializeField] private Transform raycastStartPosition;
        
        
        [SerializeField] private Transform playerHand;

        void Update()
        {
            if (isLocalPlayer)
            {
                CheckIfMoving();
            }
        }

        void CheckIfMoving()
        {
            Ray ray = new Ray(raycastStartPosition.position, gameObject.transform.forward);
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.cyan);


            if (isLocalPlayer && Input.GetKeyDown(KeyCode.P))
            {
                //手放す
                if (pickedItemGameObject != null)
                {
                    CmdRelease(pickedItemGameObject);
                    pickedItemGameObject = null;
                }

                if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask))
                {
                    //装備可能なものを見つけてたら取得
                    pickedItemGameObject = hit.transform.gameObject;
                    Debug.Log(pickedItemGameObject.name+" を装備しました");
                    //装備する
                    CmdPickUp(pickedItemGameObject);
                }
            }
        }

        [ClientRpc]
        void RpcRelease(GameObject obj)
        {
            pickedItemGameObject = null;
            obj.layer = pickUpLayer;
            obj.transform.SetParent(null);
            //親子関係を消したので、ネットワーク位置同期を復活させる
            obj.GetComponent<NetworkTransform>().enabled = true;
        }

        [Command]
        void CmdRelease(GameObject obj)
        {
            objNetId = obj.GetComponent<NetworkIdentity>(); // get the object's network ID
            objNetId.AssignClientAuthority(
                connectionToClient); // 権限の委譲


            RpcRelease(obj); // 全クライアントに、手放したことを通知する

            //例えばここで権限をもっているので色を変えたりも出来る
            //this.GetComponent<Renderer>().material.color = Color.red;
            objNetId.RemoveClientAuthority(
                connectionToClient); // remove the authority from the player who changed the color
        }

        /// <summary>
        /// ClientRpcに直接Transformは渡せないはず
        /// なのでPlayerのNetworkIdentity経由でちょっと回りくどい方法でマウント先のTransformを取得します
        /// それぞれのクライアントごとにobjを受け取っていこう
        /// </summary>
        /// <param name="willPickUpItem"></param>
        /// <param name="player"></param>
        [ClientRpc]
        void RpcPickUp(GameObject willPickUpItem, NetworkIdentity player)
        {
            Debug.Log(netId+ "が:"+willPickUpItem.name+"を拾います");

            var t = player.gameObject.transform.Find(playerHand.name);

            willPickUpItem.layer = weaponLayer;
            willPickUpItem.transform.position = t.position;
            willPickUpItem.transform.rotation = t.rotation;
            willPickUpItem.transform.SetParent(t);
            //親子関係をつけたので、ネットワーク位置同期は切って良いとおもう
            willPickUpItem.GetComponent<NetworkTransform>().enabled = false;
            pickedItemGameObject = willPickUpItem;
        }

        [Command]
        void CmdPickUp(GameObject willPickItem)
        {
            objNetId = willPickItem.GetComponent<NetworkIdentity>(); // get the object's network ID
            objNetId.AssignClientAuthority(
                connectionToClient); // 権限の取得
            
            var player = this.GetComponent<NetworkIdentity>();
            RpcPickUp(willPickItem, player); // 全クライアントに「拾ったよ」という処理を同期させる

            //this.GetComponent<Renderer>().material.color = Color.blue //権限を持っているので色を変えたりする？
            objNetId.RemoveClientAuthority(
                connectionToClient); // remove the authority from the player who changed the color
        }
    }
}