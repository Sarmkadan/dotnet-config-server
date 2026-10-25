# CollectionExtensions

Extension methods for working with `IEnumerable<T>` and related collection types, providing common operations for batching, filtering, grouping, and conversion.

## API

### `Batch<T>(this IEnumerable<T> source, int batchSize)`

Partitions the source sequence into chunks of the specified size.

- **Parameters**
  - `source`: The sequence to partition.
  - `batchSize`: The maximum number of elements per chunk. Must be greater than zero.
- **Return value**: An `IEnumerable<IEnumerable<T>>` where each inner `IEnumerable<T>` contains at most `batchSize` elements.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`. Throws `ArgumentException` if `batchSize` is less than or equal to zero.

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

### `SkipLast<T>(this IEnumerable<T> source)`

Gets all elements except the last one.

- **Parameters**
  - `source`: The sequence to enumerate.
- **Return value**: An `IEnumerable<T>` that contains all elements of the source sequence except the last one. If the sequence is empty or contains a single element, the result is empty.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

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

### `Shuffle<T>(this IEnumerable<T> source, Random? random = null)`

Returns a new sequence with the elements of the source sequence in random order.

- **Parameters**
  - `source`: The sequence to shuffle.
  - `random`: An optional random number generator. If `null`, a new `Random` instance is created.
- **Return value**: An `IEnumerable<T>` containing the shuffled elements. The result is a `List<T>` materialized when the method is called.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---

### `ZipWith<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second)`

Combines two sequences into a sequence of tuples.

- **Parameters**
  - `first`: The first sequence.
  - `second`: The second sequence.
- **Return value**: An `IEnumerable<(T1, T2)>` of tuples, where each tuple contains one element from each of the input sequences. The result ends when the shorter sequence ends.
- **Exceptions**: Throws `ArgumentNullException` if `first` or `second` is `null`.

---

### `ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null)`

Converts a sequence of key-value pairs into a dictionary.

- **Parameters**
  - `source`: The sequence of key-value pairs.
  - `comparer`: An optional equality comparer for the keys. If `null`, the default comparer for `TKey` is used.
- **Return value**: A `Dictionary<TKey, TValue>` containing the key-value pairs from the source sequence.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`. Throws `ArgumentException` if the sequence contains duplicate keys.

---

### `FirstOrDefault<T>(this IEnumerable<T> source, T? defaultValue)`

Returns the first element of the sequence, or a specified default value if the sequence is empty.

- **Parameters**
  - `source`: The sequence to enumerate.
  - `defaultValue`: The value to return if the sequence contains no elements.
- **Return value**: The first element of the sequence, or `defaultValue` if the sequence contains no elements.
- **Exceptions**: Throws `ArgumentNullException` if `source` is `null`.

---

### `ConsecutiveGroup<TKey, T>`

A helper type used by `GroupConsecutive<T, TKey>` to represent a group of consecutive elements. It implements `IGrouping<TKey, T>`.

### `ConsecutiveGroup(TKey? key, List<T> items)`

Creates a group with the specified key and elements.

- **Parameters**
  - `key`: The key shared by the elements of the group.
  - `items`: The elements of the group.

### `Key`

The key associated with a `ConsecutiveGroup`.

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

- **Thread safety**: All methods are thread-safe for concurrent reads on the source sequence. However, if the source sequence is modified by another thread during enumeration, the behavior is undefined. A `Random` instance passed to `Shuffle` is not thread-safe, so do not share one across threads.
- **Deferred execution**: `Batch`, `SkipLast`, `DistinctBy`, and `GroupConsecutive` are iterator methods. They validate their arguments and enumerate the source only when the result is iterated. `ZipWith` validates its arguments immediately but enumerates lazily. `Shuffle` and `ToDictionary` enumerate the source immediately.
- **Null handling**: Methods accepting `IEnumerable<T>` throw `ArgumentNullException` when the sequence is `null`, with the exception of `IsNullOrEmpty`, which treats `null` as an empty sequence. Methods accepting delegates throw `ArgumentNullException` if the delegate is `null`.
- **Edge cases**: `Batch` requires `batchSize` to be greater than zero. `GroupConsecutive` only merges adjacent elements, so non-adjacent elements with the same key form separate groups (for example, `1, 1, 2, 1` produces the groups `[1, 1]`, `[2]`, `[1]`).
