# inQuiry Test Suite

The purpose of this test suit is

1. To drive the design of the API
2. To provide examples on how to use the API
3. To provide regression when the API is updated

This is done by referencing the `inQuiry.dll` and provide types from a demo 
database. The tests in the test suite are heavily leaned to examples rather
than finding edge cases or providing specifications. This means that the
test suite is strongly coupled with one specific database.

This database should be provided in the future so that anyone can clone the
project and run the test suite, but for the moment you will have to request
a backup of the database from @miklund.
