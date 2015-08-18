Buildings are a hierarchy of closely interacting nodes, all of which have some specific responsibilities to ensure that other nodes assumptions are maintained.

## Building

A building is a collection of **floors**, **vertical elements** and **facades**. Floors are stacked vertically and may be overground and underground. Vertical elements are items which overlap many floors, such as a lift shaft. Facades are walls around the outside of the building, which create items such as external windows and doors.

#### Useful Types  
 - IBuilding
 - BaseBuilding

## Prerequisites
#### Facades <- Floors

Facades wrap around the outside of the building, and are prerequisites of the floors inside them. This means that the first part of a building that will be generated is the external facades - thus a silhouette of the building viewed from outside will very quickly come together (hiding generation latency).

Facades create various elements which floors must respect, for example an internal wall cannot overlap a window in a facade. When a floor is generating it is guaranteed to be surrounded on all sides by facades, this allows the floor to query facades for obstructions. 

#### Floors <- VerticalElements

Vertical elements overlap many floors, and have all of those floors as a prerequisite. This allows floors to inspect vertical elements which overlap them and to modify the vertical element. For example a floor can tell a lift shaft to create a door onto this level.

## Floor

A floor is a collection of rooms, surrounded by external facades (which will have been subdivided before the floor). A floor supplies a room (IRoom) node to any vertical elements which overlap it.

#### Useful Types
 - IFloor
 - BaseFloor

## Vertical Element

A vertical elements overlaps many floors, which will have been subdivided before the vertical element. Each floor supplies an room (IRoom) node which represents the vertical element as it exists on that floor.

#### Useful Types
 - IVerticalFeature

## Room

todo: review this...

A room is a empty space, surrounded by facades. A room (implementing IFacadeOwner) should search parents nodes for facades (using the FindFacade extension) and if this does not return anything, create a facade for itself. This searching system allows a room to ask for a facade and then for one to be returned from an element higher up in the tree which has more information - for example returning a facade which borders two rooms.

Rooms should implement **IRoom**.