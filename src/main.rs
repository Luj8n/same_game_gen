use rayon::prelude::*;

const SIZE: usize = 6;
const COLOR_COUNT: usize = 6;
const MIN_CONNECTED: usize = 6;

struct Square {
  color: usize,
  enabled: bool,
}

impl Square {
  fn click(&self, x: usize, y: usize, squares: &mut [[Square; SIZE]; SIZE]) -> bool {
    let mut visited = [[false; SIZE]; SIZE];
    let mut group: Vec<&mut Square> = vec![];

    fn recurse<'a>(
      x: isize,
      y: isize,
      color: usize,
      squares: &'a mut [[Square; SIZE]; SIZE],
      visited: &mut [[bool; SIZE]; SIZE],
      group: &mut Vec<&'a mut Square>,
    ) {
      if x < 0 || y < 0 || x >= SIZE as isize || y >= SIZE as isize || visited[y as usize][y as usize] {
        return;
      }
      visited[y as usize][y as usize] = true;

      let square = &mut squares[y as usize][y as usize];
      if square.color != color || !square.enabled {
        return;
      }

      // TODO! fix this

      group.push(square);

      recurse(x + 1, y, color, squares, visited, group);
      recurse(x - 1, y, color, squares, visited, group);
      recurse(x, y + 1, color, squares, visited, group);
      recurse(x, y - 1, color, squares, visited, group);
    }

    recurse(x as isize, y as isize, self.color, squares, &mut visited, &mut group);

    if group.len() < MIN_CONNECTED {
      return false;
    }

    for square in &mut group {
      (*square).enabled = false;
    }

    todo!()
  }
}

struct Game {
  squares: [[Square; SIZE]; SIZE],
}

impl Game {
  fn move_down(&mut self) {
    for x in 0..SIZE {
      let mut count: usize = 0;
      for y in (0..SIZE).rev() {
        let mut square = &mut self.squares[y][x];

        if !square.enabled {
          count += 1;
        } else if count > 0 {
          square.enabled = false;
          let old_color = square.color;

          let target_square = &mut self.squares[y + count][x];

          target_square.color = old_color;
          target_square.enabled = true;
        }
      }
    }
  }

  fn move_right(&mut self) {
    for x in 0..SIZE {
      let mut collumn_empty: bool = (0..SIZE).all(|y| !self.squares[y][x].enabled);
      if !collumn_empty {
        continue;
      }

      for yy in 0..SIZE {
        for xx in (1..=x).rev() {
          if self.squares[yy][xx - 1].enabled {
            self.squares[yy][xx - 1].enabled = false;
            self.squares[yy][xx].color = self.squares[yy][xx - 1].color;
            self.squares[yy][xx].enabled = true;
          }
        }
      }
    }
  }

  fn game_won(&self) -> bool {
    for y in 0..SIZE {
      for x in 0..SIZE {
        if self.squares[y][x].enabled {
          return false;
        }
      }
    }

    true
  }

  fn game_lost(&self) -> bool {
    let mut visited = [[false; SIZE]; SIZE];

    fn recurse(
      x: isize,
      y: isize,
      color: usize,
      squares: &[[Square; SIZE]; SIZE],
      visited: &mut [[bool; SIZE]; SIZE],
    ) -> usize {
      if x < 0 || y < 0 || x >= SIZE as isize || y >= SIZE as isize || visited[y as usize][y as usize] {
        return 0;
      }

      let square = &squares[y as usize][y as usize];
      if square.color != color || !square.enabled {
        return 0;
      }
      visited[y as usize][y as usize] = true;

      1 + recurse(x + 1, y, color, squares, visited)
        + recurse(x - 1, y, color, squares, visited)
        + recurse(x, y + 1, color, squares, visited)
        + recurse(x, y - 1, color, squares, visited)
    }
    for y in 0..SIZE {
      for x in 0..SIZE {
        if recurse(
          x as isize,
          y as isize,
          self.squares[y][x].color,
          &self.squares,
          &mut visited,
        ) >= MIN_CONNECTED
        {
          return false;
        }
      }
    }

    true
  }
}

fn main() {
  println!("Hello, world!");
}
