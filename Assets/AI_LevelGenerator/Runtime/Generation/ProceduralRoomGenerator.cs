using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Generation
{
    using Core;
    using Data;
    
    /// <summary>
    /// Generates procedural room meshes when no prefab is available.
    /// Creates floor, walls, ceiling, and doorways.
    /// </summary>
    public class ProceduralRoomGenerator
    {
        #region Configuration
        
        public float WallHeight { get; set; } = 3f;
        public float WallThickness { get; set; } = 0.2f;
        public float DoorWidth { get; set; } = 2f;
        public float DoorHeight { get; set; } = 2.5f;
        public bool GenerateCeiling { get; set; } = true;
        public Material FloorMaterial { get; set; }
        public Material WallMaterial { get; set; }
        public Material CeilingMaterial { get; set; }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generates a complete room GameObject with mesh.
        /// </summary>
        public GameObject GenerateRoom(RoomData room, float cellSize)
        {
            var roomObject = new GameObject($"ProceduralRoom_{room.Id}");
            roomObject.transform.position = room.WorldPosition;
            
            var size = new Vector3(cellSize, WallHeight, cellSize);
            
            // Generate floor
            var floor = GenerateFloor(size);
            floor.transform.SetParent(roomObject.transform, false);
            
            // Generate walls with doorways
            var walls = GenerateWalls(size, room.Connections);
            walls.transform.SetParent(roomObject.transform, false);
            
            // Generate ceiling
            if (GenerateCeiling)
            {
                var ceiling = CreateCeilingMesh(size);
                ceiling.transform.SetParent(roomObject.transform, false);
            }
            
            // Add collider for room detection
            var boxCollider = roomObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, WallHeight / 2f, 0);
            boxCollider.size = new Vector3(cellSize, WallHeight, cellSize);
            boxCollider.isTrigger = true;
            
            return roomObject;
        }
        
        /// <summary>
        /// Generates floor only.
        /// </summary>
        public GameObject GenerateFloor(Vector3 size)
        {
            var floor = new GameObject("Floor");
            var meshFilter = floor.AddComponent<MeshFilter>();
            var meshRenderer = floor.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = CreateQuadMesh(size.x, size.z);
            meshRenderer.material = FloorMaterial ?? CreateDefaultMaterial(new Color(0.4f, 0.4f, 0.4f));
            
            // Position at bottom
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            return floor;
        }
        
        /// <summary>
        /// Generates walls with doorways.
        /// </summary>
        public GameObject GenerateWalls(Vector3 size, ConnectionDirection doors)
        {
            var wallsParent = new GameObject("Walls");
            
            // North wall
            if (!doors.HasFlag(ConnectionDirection.North))
            {
                CreateWall(wallsParent.transform, "WallNorth", 
                    new Vector3(0, WallHeight / 2f, size.z / 2f),
                    new Vector3(size.x, WallHeight, WallThickness));
            }
            else
            {
                CreateWallWithDoor(wallsParent.transform, "WallNorth",
                    new Vector3(0, WallHeight / 2f, size.z / 2f),
                    size.x, WallHeight, WallThickness, true);
            }
            
            // South wall
            if (!doors.HasFlag(ConnectionDirection.South))
            {
                CreateWall(wallsParent.transform, "WallSouth",
                    new Vector3(0, WallHeight / 2f, -size.z / 2f),
                    new Vector3(size.x, WallHeight, WallThickness));
            }
            else
            {
                CreateWallWithDoor(wallsParent.transform, "WallSouth",
                    new Vector3(0, WallHeight / 2f, -size.z / 2f),
                    size.x, WallHeight, WallThickness, true);
            }
            
            // East wall
            if (!doors.HasFlag(ConnectionDirection.East))
            {
                CreateWall(wallsParent.transform, "WallEast",
                    new Vector3(size.x / 2f, WallHeight / 2f, 0),
                    new Vector3(WallThickness, WallHeight, size.z));
            }
            else
            {
                CreateWallWithDoor(wallsParent.transform, "WallEast",
                    new Vector3(size.x / 2f, WallHeight / 2f, 0),
                    size.z, WallHeight, WallThickness, false);
            }
            
            // West wall
            if (!doors.HasFlag(ConnectionDirection.West))
            {
                CreateWall(wallsParent.transform, "WallWest",
                    new Vector3(-size.x / 2f, WallHeight / 2f, 0),
                    new Vector3(WallThickness, WallHeight, size.z));
            }
            else
            {
                CreateWallWithDoor(wallsParent.transform, "WallWest",
                    new Vector3(-size.x / 2f, WallHeight / 2f, 0),
                    size.z, WallHeight, WallThickness, false);
            }
            
            return wallsParent;
        }
        
        /// <summary>
        /// Generates ceiling.
        /// </summary>
        /// <summary>
        /// Generates ceiling.
        /// </summary>
        public GameObject CreateCeilingMesh(Vector3 size)
        {
            var ceiling = new GameObject("Ceiling");
            var meshFilter = ceiling.AddComponent<MeshFilter>();
            var meshRenderer = ceiling.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = CreateQuadMesh(size.x, size.z);
            meshRenderer.material = CeilingMaterial ?? CreateDefaultMaterial(new Color(0.3f, 0.3f, 0.35f));
            
            ceiling.transform.localPosition = new Vector3(0, WallHeight, 0);
            ceiling.transform.localRotation = Quaternion.Euler(180, 0, 0);
            
            return ceiling;
        }
        
        /// <summary>
        /// Generates a corridor segment mesh.
        /// </summary>
        public GameObject GenerateCorridor(Vector3 start, Vector3 end, float width)
        {
            var corridor = new GameObject("Corridor");
            corridor.transform.position = (start + end) / 2f;
            
            var direction = (end - start).normalized;
            var length = Vector3.Distance(start, end);
            
            corridor.transform.rotation = Quaternion.LookRotation(direction);
            
            // Floor
            var floor = new GameObject("Floor");
            floor.transform.SetParent(corridor.transform, false);
            floor.AddComponent<MeshFilter>().mesh = CreateQuadMesh(width, length);
            floor.AddComponent<MeshRenderer>().material = FloorMaterial ?? CreateDefaultMaterial(new Color(0.35f, 0.35f, 0.35f));
            floor.transform.localRotation = Quaternion.Euler(0, 90, 0);
            
            // Left wall
            var leftWall = CreateWallMesh(length, WallHeight, WallThickness);
            leftWall.transform.SetParent(corridor.transform, false);
            leftWall.transform.localPosition = new Vector3(-width / 2f, WallHeight / 2f, 0);
            leftWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
            
            // Right wall
            var rightWall = CreateWallMesh(length, WallHeight, WallThickness);
            rightWall.transform.SetParent(corridor.transform, false);
            rightWall.transform.localPosition = new Vector3(width / 2f, WallHeight / 2f, 0);
            rightWall.transform.localRotation = Quaternion.Euler(0, -90, 0);
            
            // Ceiling
            if (GenerateCeiling)
            {
                var ceilingObj = new GameObject("Ceiling");
                ceilingObj.transform.SetParent(corridor.transform, false);
                ceilingObj.AddComponent<MeshFilter>().mesh = CreateQuadMesh(width, length);
                ceilingObj.AddComponent<MeshRenderer>().material = CeilingMaterial ?? CreateDefaultMaterial(new Color(0.3f, 0.3f, 0.35f));
                ceilingObj.transform.localPosition = new Vector3(0, WallHeight, 0);
                ceilingObj.transform.localRotation = Quaternion.Euler(180, 90, 0);
            }
            
            return corridor;
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = size;
            
            var renderer = wall.GetComponent<Renderer>();
            renderer.material = WallMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
        }
        
        private void CreateWallWithDoor(Transform parent, string name, Vector3 position, float width, float height, float thickness, bool horizontal)
        {
            var wallSet = new GameObject(name);
            wallSet.transform.SetParent(parent, false);
            wallSet.transform.localPosition = position;
            
            float sideWidth = (width - DoorWidth) / 2f;
            float topHeight = height - DoorHeight;
            
            // Left/bottom side
            var side1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            side1.name = "Side1";
            side1.transform.SetParent(wallSet.transform, false);
            if (horizontal)
            {
                side1.transform.localPosition = new Vector3(-(DoorWidth + sideWidth) / 2f, 0, 0);
                side1.transform.localScale = new Vector3(sideWidth, height, thickness);
            }
            else
            {
                side1.transform.localPosition = new Vector3(0, 0, -(DoorWidth + sideWidth) / 2f);
                side1.transform.localScale = new Vector3(thickness, height, sideWidth);
            }
            side1.GetComponent<Renderer>().material = WallMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
            
            // Right/top side
            var side2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            side2.name = "Side2";
            side2.transform.SetParent(wallSet.transform, false);
            if (horizontal)
            {
                side2.transform.localPosition = new Vector3((DoorWidth + sideWidth) / 2f, 0, 0);
                side2.transform.localScale = new Vector3(sideWidth, height, thickness);
            }
            else
            {
                side2.transform.localPosition = new Vector3(0, 0, (DoorWidth + sideWidth) / 2f);
                side2.transform.localScale = new Vector3(thickness, height, sideWidth);
            }
            side2.GetComponent<Renderer>().material = WallMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
            
            // Top piece above door
            if (topHeight > 0)
            {
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "Top";
                top.transform.SetParent(wallSet.transform, false);
                if (horizontal)
                {
                    top.transform.localPosition = new Vector3(0, (height - topHeight) / 2f + topHeight / 2f - height / 2f + DoorHeight, 0);
                    top.transform.localScale = new Vector3(DoorWidth, topHeight, thickness);
                }
                else
                {
                    top.transform.localPosition = new Vector3(0, (height - topHeight) / 2f + topHeight / 2f - height / 2f + DoorHeight, 0);
                    top.transform.localScale = new Vector3(thickness, topHeight, DoorWidth);
                }
                top.GetComponent<Renderer>().material = WallMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
            }
        }
        
        private GameObject CreateWallMesh(float width, float height, float thickness)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.localScale = new Vector3(thickness, height, width);
            wall.GetComponent<Renderer>().material = WallMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
            return wall;
        }
        
        private Mesh CreateQuadMesh(float width, float height)
        {
            var mesh = new Mesh();
            mesh.name = "Quad";
            
            float halfW = width / 2f;
            float halfH = height / 2f;
            
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfW, 0, -halfH),
                new Vector3(halfW, 0, -halfH),
                new Vector3(halfW, 0, halfH),
                new Vector3(-halfW, 0, halfH)
            };
            
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(width, 0),
                new Vector2(width, height),
                new Vector2(0, height)
            };
            
            mesh.normals = new Vector3[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };
            
            mesh.RecalculateBounds();
            return mesh;
        }
        
        private Material CreateDefaultMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            return material;
        }
        
        #endregion
    }
}
