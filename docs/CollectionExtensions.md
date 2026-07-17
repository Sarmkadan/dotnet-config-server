# CollectionExtensions

Extension methods for working with `IEnumerable<T>` and related collection types, providing common operations for batching, filtering, grouping, and conversion.

## API

### `Batch<T>(this IEnumerable<T> source, int size)`

Partitions the source sequence into chunks of the specified size.

- **Parameters**
  - `source`: The sequence to partition.
  - `size`: The maximum number of elements per chunk.
- **Return value**: An `IEnumerable<IEnumerable<T>>` where each inner `IEnumerable<T>` contains at most `size` elements.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`. Throws `ArgumentOutOfRangeException` if `size` is less than 1.

---

### `ForEach<T>(this IEnumerable<T> source, Action<T> action)`

Applies the given action to each element in the sequence.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `action`: The delegate to invoke for each element.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `action` is `null`.

---

### `ForEach<T>(this IEnumerable<T> source, Action<T, int> action)`

Applies the given action to each element in the sequence, providing the zero-based index of the element.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `action`: The delegate to invoke for each element, receiving the element and its index.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `action` is `null`.

---

### `IsNullOrEmpty<T>(this IEnumerable<T>? source)`

Determines whether the sequence is `null` or contains no elements.

- **Parameters**
  - `source`: The sequence to check.
- **Return value**: `true` if `source` is `null` or empty; otherwise, `false`.

---

### `IsSingle<T>(this IEnumerable<T> source)`

Determines whether the sequence contains exactly one element.

- **Parameters**
  - `source`: The sequence to check.
- **Return value**: `true` if `source` contains exactly one element; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---

### `HasMultiple<T>(this IEnumerable<T> source)`

Determines whether the sequence contains two or more elements.

- **Parameters**
  - `source`: The sequence to check.
- **Return value**: `true` if `source` contains two or more elements; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---
### `SkipLast<T>(this IEnumerable<T> source, int count)`

Bypasses a specified number of elements at the end of the sequence and returns the remaining elements.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `count`: The number of elements to skip from the end.
- **Return value**: An `IEnumerable<T>` that contains the elements that occur before the specified count near the end of the input sequence.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`. Throws `ArgumentOutOfRangeException` if `count` is less than 0.

---
### `DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`

Returns distinct elements from the sequence based on a key selector function.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `keySelector`: A function to extract the key for each element.
- **Return value**: An `IEnumerable<T>` that contains distinct elements from the source sequence.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `keySelector` is `null`.

---
### `GroupConsecutive<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`

Groups consecutive elements that share the same key.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `keySelector`: A function to extract the key for each element.
- **Return value**: An `IEnumerable<IGrouping<TKey, T>>` where each group contains consecutive elements with the same key.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `keySelector` is `null`.

---
### `Shuffle<T>(this IEnumerable<T> source)`

Returns a new sequence with the elements of the source sequence in random order.

- **Parameters**
  - `source`: The sequence to shuffle.
- **Return value**: An `IEnumerable<T>` containing the shuffled elements.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---
### `ZipWith<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second)`

Combines two sequences into a sequence of tuples.

- **Parameters**
  - `first`: The first sequence.
  - `second`: The second sequence.
- **Return value**: An `IEnumerable<(T1, T2)>` of tuples, where each tuple contains one element from each of the input sequences.
- **Exceptions**: Throws `ArgumentNullException` if `first` or `second` is `null`.

---
### `ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)`

Converts a sequence of key-value pairs into a dictionary.

- **Parameters**
  - `source`: The sequence of key-value pairs.
- **Return value**: A `Dictionary<TKey, TValue>` containing the key-value pairs from the source sequence.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`. Throws `ArgumentException` if the sequence contains duplicate keys.

---
### `FirstOrDefault<T>(this IEnumerable<T> source)`

Returns the first element of the sequence, or a default value if the sequence is empty.

- **Parameters**
  - `source`: The sequence to enumerate.
- **Return value**: The first element of the sequence, or `default(T)` if the sequence contains no elements.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---
### `ConsecutiveGroup`

A helper type used by `GroupConsecutive<T, TKey>` to represent a group of consecutive elements.

---
### `Key`

The key associated with a `ConsecutiveGroup`.

---
### `GetEnumerator()`

Returns an enumerator that iterates through the `ConsecutiveGroup`.

- **Return value**: An `IEnumerator<T>` for the `ConsecutiveGroup`.

## Usage

```csharp
// Batch a large sequence into manageable chunks
var numbers = Enumerable.Range(1, 100);
foreach (var batch in numbers.Batch(10))
{
    Console.WriteLine($"Batch: {string.Join(", ", batch)}");
}

// Group consecutive identical values
var data = new[] { 1, 1, 2, 3, 3, 3, 2, 2 };
var groups = data.GroupConsecutive(x => x);
foreach (var group in groups)
{
    Console.WriteLine($"{group.Key}: {string.Join(", ", group)}");
}
```

## Notes

- **Thread safety**: All methods are thread-safe for concurrent reads on the source sequence. However, if the source sequence is modified by another thread during enumeration, the behavior is undefined.
- **Deferred execution**: Methods returning `IEnumerable<T>` use deferred execution and will enumerate the source sequence only when the result is iterated.
- **Null handling**: Methods accepting `IEnumerable<T>` treat `null` as an empty sequence unless otherwise specified. Methods accepting delegates throw `ArgumentNullException` if the delegate is `null`.
- **Edge cases**: Methods like `Batch`, `SkipLast`, and `DistinctBy` validate parameters strictly (e.g., `size` must be positive). Methods like `IsSingle` and `HasMultiple` return `false` for `null` input.
