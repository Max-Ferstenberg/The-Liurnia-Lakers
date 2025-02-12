def transformString(input):
    arr = input.lower().split()
    for i in range (len(arr)):
        if arr[i] in arr[i:]:
            arr[i] = 'y'
        else:
            arr[i] = 'x'
    
    return ''.join(arr)

if __name__ == "__main__":
    print(transformString("hello world"))