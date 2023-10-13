# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2023-10-13

## Added

- NativePriorityQueue can be deallocated on job completion

## Fixed

- Removed attribute for parallel writing
- Moved static safety id initialization to its own method to fix burst error

## [1.0.1] - 2023-09-03

## Fixed

- Fixed a few spelling mistakes and comments

## [1.0.0] - 2023-09-03

### Added

- NativePriorityQueue (using MinHeap as background data structure)