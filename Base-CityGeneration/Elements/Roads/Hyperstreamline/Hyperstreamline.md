## Hyperstreamline

Hyperstreamline is a system for generating road networks based on a set of user created design elements. It is based on [Interactive Procedural Street Modelling](http://www.peterwonka.net/Publications/pdfs/2008.SG.Chen.InteractiveProceduralStreetModeling.pdf). Design elements are hand placed by the user, and influence the general flow of roads in an area.

## Example Layout

    !Network
    Aliases:
        - &base-field !AddTensors
          Tensors:
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0 }, Decay: 2.5, Center: { X: 0, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45 }, Decay: 2.5, Center: { X: 1, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45 }, Decay: 2.5, Center: { X: 0, Y: 1 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0 }, Decay: 2.5, Center: { X: 1, Y: 1 } }
    
    Major:
        MergeSearchAngle: 22.5
        MergeDistance: 25
        SegmentLength: 10
        RoadWidth: !NormalValue { Min: 2, Max: 4, Vary: true }
        PriorityField: !ConstantScalars { Value: 1 }
        SeparationField: !ConstantScalars { Value: 50 }
        TensorField: *base-field
    
    Minor:
        MergeSearchAngle: 12.5
        MergeDistance: 2.5
        SegmentLength: 2
        RoadWidth: !NormalValue { Min: 1, Max: 2, Vary: true }
        PriorityField: !ConstantScalars { Value: 1 }
        SeparationField: !ConstantScalars { Value: 25 }
        TensorField:
            !WeightedAverage
            Tensors:
                0.1: *base-field
                0.9: !Grid { Angle: !UniformValue { Min: 1, Max: 360, Vary: true } }

All networks begin with the *!Network* tag.

### Aliases
This is a list of objects which is completely ignored after deserialization is completed. This is a convenient place to create *named objects* (using the *&name* syntax) which can be referred to later in the file (using the **name* syntax).

### Major/Minor
This is the configuration for major and minor roads. The major configuration is evaluated once, and then a street network is generated from it. Enclosed regions of space are extracted from this network. This minor configuration is evaluated once for every single region, and then roads are traced within this region.

##### MergeSearchAngle
When a road is being traced it can merge onto roads within a narrow cone in front of it. This parameter determines the angular width of the cone.

##### MergeDistance
This parameter determines the length of the cone (see MergeSearchAngle).

##### SegmentLength
When a road is being traced it will be traced in segments this long. This value should be smaller than the merge distance.

##### RoadWidth
When a road has been completely traced it will be assigned a width (the number of lanes going in one direction). This parameter is a value generator which is evaluated once per road and determines the width.

##### PriorityField
This is a scalar field which determines the priority of road generation at each point.

##### SeparationField
This is a scalar field which determines how far apart roads should be at each point.

##### TensorField
This is a tensor field which determines the flow of roads at each point.