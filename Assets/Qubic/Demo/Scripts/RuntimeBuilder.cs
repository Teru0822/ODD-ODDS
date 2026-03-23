using System.Linq;
using UnityEngine;

namespace QubicNS
{
    public class RuntimeBuilder : MonoBehaviour
    {
        [SerializeField] QubicBuilder Builder;
        [SerializeField] GameObject PlaceholderPrefab;
        BaseRoom currentRoom;
        GameObject placeHolder;

        private void Start()
        {
            // create placeholder
            if (placeHolder == null && PlaceholderPrefab != null)
                placeHolder = Instantiate(PlaceholderPrefab);

            if (Builder == null)
                return;

            if (currentRoom == null)
                currentRoom = Builder?.GetComponentsInChildren<BaseRoom>().LastOrDefault();

            Rebuild();
        }

        private void OnDisable()
        {
            if (placeHolder != null)
                placeHolder.SetActive(false);
        }

        private void Update()
        {
            ProcessMouse();
        }

        private void ProcessMouse()
        {
            if (Builder == null || currentRoom == null)
            {
                if (placeHolder != null)
                    placeHolder.SetActive(false);
                return;
            }

            placeHolder.SetActive(true);
            placeHolder.transform.localScale = new Vector3(Builder.CellSize, 0.1f, Builder.CellSize);

            // get intersection point with ZX plane
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, 0).Raycast(ray, out var d))
            {
                var pos = ray.GetPoint(d) + Vector3.up / 10f;

                // get cell index
                var cell = Builder.Map.PosToCell(pos);

                // set placeholder position
                placeHolder.transform.position = Builder.Map.CellToPos(cell);

                // get cell index relative to room
                var relative = currentRoom.GetRelativeCellHex(cell);

                if (!Helper.IsMouseOverGUI)
                {
                    // add cell to room
                    if (Input.GetMouseButton(0))
                    if (currentRoom.AddCustomCell(relative))
                        Rebuild();

                    // remove cell from room
                    if (Input.GetMouseButton(1))
                    if (currentRoom.RemoveCustomCell(relative))
                        Rebuild();
                }
            }
        }

        void Rebuild()
        {
            var e = Builder.BuildInternal();
            while (e.Enumerate()) ;
        }

        public void NewRoom()
        {
            // create room
            var room = Templates.Room();
            room.transform.SetParent(Builder.transform, false);

            // use tag Room for cells
            room.Tags = "Room";

            // remove init cell (becuase we will draw cells manually)
            room.RemoveCustomCell(Vector3Int.zero);
            
            currentRoom = room;
            Rebuild();
        }
    }
}