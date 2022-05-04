# Filter

## How to use implemented algorithms

In [Filter.cs](Assets/Filter.cs) find

```cs
Vector3 filtered = ...;
```

In methods

```cs
public Vector3 FilterPosition
```

```cs
public Vector3 FilterRotation
```

And change the filtering algorithm.

### Median

```cs
Vector3 filtered = Median(postWindow);
```

### Kolmogorov Zurbenko filter

```cs
Vector3 filtered = KolZur(postWindow, 3);
```

### LULU smoothing

L and U operators decreases the ArrayList in `2 * n` elements.
Resulting element is in `resulting_array_length / 2` position.
For example:

```cs
// (CONSID_ELEMS - 1 * 2 - 1 * 2) / 2 = (11 - 4) / 2 = 3
Vector3 filtered = (Vector3) L(U(postWindow, 1), 1)[3];
```

## Plot graphics

See [8_Plots](8_Plots.ipynb)