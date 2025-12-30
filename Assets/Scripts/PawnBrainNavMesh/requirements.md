Sometimes it is necessary to let the agent navigate across places which are not walkable, for example, jumping over a fence, or traversing through a closed door. These cases need to know the location of the action.

You can annotate these actions with the NavMesh Link component, which tells the pathfinder that a route exists through the specified link. Your code can later access this link and perform the special action as the agent follows the path.
