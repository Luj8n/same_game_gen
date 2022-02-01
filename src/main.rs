use itertools::Itertools;
use rayon::prelude::*;

const SIZE: usize = 8;
const COLOR_COUNT: usize = 8;
const MIN_CONNECTED: usize = 2;

#[derive(Clone, Copy)]
struct Square {
  color: usize,
  enabled: bool,
}

impl Default for Square {
  fn default() -> Self {
    Square {
      color: 0,
      enabled: true,
    }
  }
}

impl Square {
  fn new(color: usize) -> Self {
    Square { color, enabled: true }
  }
}

#[derive(Clone)]
struct Game {
  squares: [[Square; SIZE]; SIZE],
}

impl ToString for Game {
  fn to_string(&self) -> String {
    self
      .squares
      .iter()
      .map(|r| {
        r.iter()
          .map(|x| {
            if x.enabled {
              x.color.to_string()
            } else {
              " ".to_string()
            }
          })
          .join(" ")
      })
      .join("\n")
  }
}

impl Game {
  fn new(squares: [[Square; SIZE]; SIZE]) -> Self {
    Game { squares }
  }

  fn click(&mut self, x: usize, y: usize) -> bool {
    let mut visited = [[false; SIZE]; SIZE];
    let mut group: Vec<(usize, usize)> = vec![];

    fn recurse(
      x: isize,
      y: isize,
      color: usize,
      game: &Game,
      visited: &mut [[bool; SIZE]; SIZE],
      group: &mut Vec<(usize, usize)>,
    ) {
      if x < 0 || y < 0 || x >= SIZE as isize || y >= SIZE as isize || visited[y as usize][x as usize] {
        return;
      }
      visited[y as usize][x as usize] = true;

      let square = &game.squares[y as usize][x as usize];
      if square.color != color || !square.enabled {
        return;
      }

      group.push((x as usize, y as usize));

      recurse(x + 1, y, color, game, visited, group);
      recurse(x - 1, y, color, game, visited, group);
      recurse(x, y + 1, color, game, visited, group);
      recurse(x, y - 1, color, game, visited, group);
    }

    recurse(
      x as isize,
      y as isize,
      self.squares[y][x].color,
      self,
      &mut visited,
      &mut group,
    );

    if group.len() < MIN_CONNECTED {
      return false;
    }

    for (x, y) in group {
      self.squares[y][x].enabled = false;
    }

    true
  }

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
      // check if collumn is empty
      if (0..SIZE).any(|y| self.squares[y][x].enabled) {
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

  // fn game_lost(&self) -> bool {
  //   let mut visited = [[false; SIZE]; SIZE];

  //   fn recurse(
  //     x: isize,
  //     y: isize,
  //     color: usize,
  //     squares: &[[Square; SIZE]; SIZE],
  //     visited: &mut [[bool; SIZE]; SIZE],
  //   ) -> usize {
  //     if x < 0 || y < 0 || x >= SIZE as isize || y >= SIZE as isize || visited[y as usize][y as usize] {
  //       return 0;
  //     }

  //     let square = &squares[y as usize][y as usize];
  //     if square.color != color || !square.enabled {
  //       return 0;
  //     }
  //     visited[y as usize][y as usize] = true;

  //     1 + recurse(x + 1, y, color, squares, visited)
  //       + recurse(x - 1, y, color, squares, visited)
  //       + recurse(x, y + 1, color, squares, visited)
  //       + recurse(x, y - 1, color, squares, visited)
  //   }
  //   for y in 0..SIZE {
  //     for x in 0..SIZE {
  //       if recurse(
  //         x as isize,
  //         y as isize,
  //         self.squares[y][x].color,
  //         &self.squares,
  //         &mut visited,
  //       ) >= MIN_CONNECTED
  //       {
  //         return false;
  //       }
  //     }
  //   }

  //   true
  // }
}

fn main() {
  let rng = fastrand::Rng::new();
  let mut levels: Vec<(Game, Vec<(usize, usize)>)> = vec![];

  while levels.len() < 1 {
    let mut squares = [[Square::default(); SIZE]; SIZE];
    let mut used_colors = [false; COLOR_COUNT];

    for y in 0..SIZE {
      for x in 0..SIZE {
        let color = rng.usize(0..COLOR_COUNT);
        used_colors[color] = true;

        squares[y][x].color = color;
      }
    }

    let mut all_colors_used = true;

    for i in 0..COLOR_COUNT {
      if !used_colors[i] {
        all_colors_used = false;
        break;
      }
    }

    if !all_colors_used {
      continue;
    }

    let mut game = Game::new(squares);
    let game_save = game.clone();

    let mut game_won = false;
    let mut clicks: Vec<(usize, usize)> = vec![];
    let mut tries = 0_usize;

    while clicks.len() < (SIZE * SIZE) / MIN_CONNECTED && tries < SIZE * SIZE {
      let x = rng.usize(0..SIZE);
      let y = rng.usize(0..SIZE);

      let removed = game.click(x, y);

      if !removed {
        tries += 1;
        continue;
      }

      tries = 0;
      clicks.push((x, y));

      game.move_down();
      game.move_right();

      // println!("{}", game.to_string());
      // println!("-------------");

      if game.game_won() {
        game_won = true;
        break;
      }
    }

    if game_won {
      levels.push((game_save, clicks));
    }
  }
}
