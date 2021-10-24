# DifferenceComparer
DifferenceComparer can be used to generate and apply differences between data entries and data difference entries (with the same schema).

The main idea and application behind the DifferenceComparer is to be able to work with differences instead of whole data states.
For example take the following context:
We are interested in writing database compare integration tests for a batch run (or any other kind of procedure that modifies the database state).
So let's say we have a database with state D0 and after the batch run it's in state D0'.
We want to add regression tests to detect database changes in case we change the batch.
I.e. if change the batch and run it and we now get a state D0'' we are interested in the difference between D0' and D0''.
Such a test is usually setup by maintaining both D0 (to be able to start the batch on that state) and D0' (to be able to compare the result D0'' to D0').
But if the database is rather large and the difference (D0 to D0' but also D0' to D0'') is rather small (at least compared to D0, which is usually the case)
this requires a lot of space.
Instead we could do the following:
We still store D0 (to be able to run the test).
But instead of storing D0' we store the difference between D0 and D0' (let's call it Diff').
And instead of comparing D0' to D0'' we "compare Diff' to Diff''".
First off this saves a lot of space and in addition this comparison directly stores (and shows) the deviations from the original batch changes.
Which can be more meaningful/useful especially for regression tests.

So the main challenge is to deduce the difference between D0' to D0'' from the Diff' and Diff''.
This is exactly the main functionality of `DifferenceComparer`.

# Syntax
![Difference progression diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/main/doc/differenceProgression.puml)
![Difference squash diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/main/doc/differenceSquash.puml)

All that's required is an id equality comparer for entries to be able to match entries
and a more strict equality comparer for entries to be able to check if they're different.
If no equality comparer is provided then the default equality comparer for the entries is used.

# Limitations
- `DifferenceComparer` assumes that the schema remains unchanged.
- Currently `DifferenceComparer` doesn't really provide data (de)serialization or loading mechanisms (for both data entries and difference entries).
- At the moment no "nice" difference representation (or (de)serialization) is supported.