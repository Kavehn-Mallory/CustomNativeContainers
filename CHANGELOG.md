# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

- NativePriorityQueue that uses a comparer instead of forcing the type T to implement IComparable&lt;T&gt;


## [1.1.0] - 2023-10-17

### Added

- FindElement() searches for an element and return the index if element is found. Returns -1 otherwise
- ReplaceElement() replaces the element at the given index with the new element
and restores the Priorities if necessary 

### Changed

- NativePriorityQueue is now using a data container for data storage to properly support Dispose with a job handle
- Reverted requirement of type T back to unmanaged, the usage of the BurstCompatible-attribute should have fixed the error

### Fixed

- Error when trying to use Dispose with a job handle

## [1.0.4] - 2023-10-13

### Changed

- Changed requirement for type T of NativePriorityQueue<T> from unmanaged to struct to prevent burst error

## [1.0.3] - 2023-10-13

### Changed

- Changed requirement for type T of NativePriorityQueue<T> from IComparable to IComparable&lt;T&gt;

## [1.0.2] - 2023-10-13

### Added

- NativePriorityQueue can be deallocated on job completion

### Fixed

- Removed attribute for parallel writing
- Moved static safety id initialization to its own method to fix burst error

## [1.0.1] - 2023-09-03

### Fixed

- Fixed a few spelling mistakes and comments

## [1.0.0] - 2023-09-03

### Added

- NativePriorityQueue (using MinHeap as background data structure)