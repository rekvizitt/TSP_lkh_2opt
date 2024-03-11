import sys
# for portability
sys.path.append('./lib')

import elkai
import ast

filename = sys.argv[1]

# evaluate file content to parse it to distances matrix
distances = ast.literal_eval(open(filename, "r").read())

print(distances)

edges = elkai.DistanceMatrix(distances)

solution = edges.solve_tsp(runs=int(sys.argv[2]))

# solution is looped by default
solution.pop()

# lets say we go from city i to city j, then the distance between them is distances[i][j]
# so for each current city in solution we find distances[current][next] and sum them to get total 
total_cost = sum(distances[solution[i]][solution[i + 1]] for i in range(len(solution) - 1))

# for 1-based indexing
for i in range(len(solution)):
    solution[i] = solution[i] + 1

with open(filename, 'w') as file:
    file.write(str(solution) + "\n")
    file.write(str(total_cost))