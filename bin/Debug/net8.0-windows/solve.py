import sys
import elkai

# Проверяем, что переданы необходимые аргументы
if len(sys.argv) < 3:
    print("Usage: python your_script.py <runs> <distances>")
    sys.exit(1)

# Получаем аргументы из командной строки
runs = int(sys.argv[1])
distances_input = sys.argv[2]

# Преобразуем строку с расстояниями в список списков
distances = []
try:
    for line in distances_input.split(';'):
        distances.append([int(num) for num in line.split(',')])
except ValueError:
    print("Error: Invalid distance value. Please ensure all distances are integers.")
    sys.exit(1)

# Создаем матрицу расстояний
edges = elkai.DistanceMatrix(distances)

# Решаем задачу коммивояжера
solution = edges.solve_tsp(runs)

# Удаляем последний элемент, если он является циклом
solution.pop()

# Считаем общую стоимость
total_cost = sum(distances[solution[i]][solution[i + 1]] for i in range(len(solution) - 1))

print(str(solution))