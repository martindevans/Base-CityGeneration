Buildings are a hierarchy of closely interacting nodes, all of which have some specific responsibilities to ensure that other nodes assumptions are maintained.

## Building

A building (generally just using **BaseBuilding**) is a collection of floors and vertical elements. Floors are stacked vertically and may be overground and underground. A building *should* fill itself in entirely solid when it is subdivided, this will allow the world generator to very quickly establish a solid silhouette of the building without having to subdivide every single floor. A building *must* set floors to be prerequisites of vertical elements.

## Vertical Element

A vertical element is anything inside a building which cross multiple floors, for example stair wells, lifts, cable channels and ventilation ducts. A vertical element will be supplied with a set of room room nodes (one for each floor which it overlaps). A vertical element *should* set itself as a prerequisite for the rooms which form it.

Vertical elements should implement from **IVerticalFeature**.

## Floor

A floor is a collection of rooms. A floor *must* cut itself out from the world when it subdivides (based on the assumption that a building will have filled in the area of the building entirely solid first). A floor *must* supply a room node to vertical elements which overlap the floor.

Floors should implement from **IFloor**. **BaseFloor** is a very flexible implementation of a floor based off a 2D top down floor plan.

## Room

A room is a empty space, surrounded by facades. A room (implementing IFacadeOwner) should search parents nodes for facades (using the FindFacade extension) and if this does not return anything, create a facade for itself. This searching system allows a room to ask for a facade and then for one to be returned from an element higher up in the tree which has more information - for example returning a facade which borders two rooms.

Rooms should implement **IRoom**.