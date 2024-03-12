import elkai

filename = './temp.txt'
runs = 0
distances = []

with open(filename, 'r', encoding='utf-8-sig') as file:
    lines = file.readlines()
    runs = int(lines[0])

    for line in lines[1:]:
        distances.append([int(num) for num in line.strip().split(', ')])

edges = elkai.DistanceMatrix(distances)

solution = edges.solve_tsp(runs=runs)

# solution is looped by default
solution.pop()

# lets say we go from city i to city j, then the distance between them is distances[i][j]
# so for each current city in solution we find distances[current][next] and sum them to get total 
total_cost = sum(distances[solution[i]][solution[i + 1]] for i in range(len(solution) - 1))

with open(filename, 'w') as file:
    file.write(str(solution) + '\n')